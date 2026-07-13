using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Usuarios.Api.Aplicacao;
using Usuarios.Api.Contratos;
using Usuarios.Api.Dominio;
using Usuarios.Api.Mensageria;
using Usuarios.Api.Persistencia;
using Usuarios.Api.Validacao;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<UsuariosDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("UsuariosDb")));

builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddSingleton<JwtTokenGenerator>();
builder.Services.AddSingleton<IGoogleIdTokenValidator, GoogleIdTokenValidator>();
builder.Services.AddScoped<RefreshTokenService>();
builder.Services.AddScoped<AuthService>();

// Convite de apoio (BACKLOG-PRODUTO.md, Sprint 7) - primeira vez que
// Usuarios.Api publica evento (antes só consumia via Gateway/JWT).
builder.Services.AddScoped<IApoioRepository, ApoioRepository>();
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddSingleton<RabbitMqConnection>();
builder.Services.AddHostedService<ApoioWorker>();
builder.Services.AddHostedService<OutboxPublisherService>();

// Validators são stateless — singleton evita recriar a cada request.
builder.Services.AddSingleton<IValidator<RegistrarRequest>, RegistrarRequestValidator>();
builder.Services.AddSingleton<IValidator<LoginRequest>, LoginRequestValidator>();
builder.Services.AddSingleton<IValidator<AtualizarPerfilRequest>, AtualizarPerfilRequestValidator>();
builder.Services.AddSingleton<IValidator<TrocarSenhaRequest>, TrocarSenhaRequestValidator>();
builder.Services.AddSingleton<IValidator<LoginGoogleRequest>, LoginGoogleRequestValidator>();
builder.Services.AddSingleton<IValidator<RenovarTokenRequest>, RenovarTokenRequestValidator>();
builder.Services.AddSingleton<IValidator<PerfilOnboardingRequest>, PerfilOnboardingRequestValidator>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Sem isso, o handler remapeia "sub"/"email" pra URIs longas do
        // WS-Federation (ClaimTypes.NameIdentifier etc) - mantemos os claims
        // exatamente como foram emitidos, mais previsível de ler depois.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<UsuariosDbContext>("postgres");

builder.Services.AddOpenApi();

var app = builder.Build();

// Aplica migrations pendentes no boot - o deploy (Render) só sobe o
// código a cada push, não roda `dotnet ef database update` sozinho.
// Sem isso, um PR que inclua migration quebra produção em silêncio até
// alguém rodar a migration manualmente (ver README, "Migração automática
// no boot"). Migrate() é idempotente - não faz nada se já estiver em dia.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<UsuariosDbContext>().Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Render (e a maioria dos PaaS gratuitos) termina TLS no proxy dele e
// repassa a requisição em HTTP puro pro container - sem isso,
// UseHttpsRedirection() entraria em loop de redirecionamento.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/registrar", async (RegistrarRequest req, AuthService auth, CancellationToken ct) =>
{
    try
    {
        var resposta = await auth.RegistrarAsync(req.Nome, req.Email, req.Senha, ct);
        return Results.Created("/me", resposta);
    }
    catch (EmailJaExisteException ex)
    {
        return Results.Conflict(new { erro = ex.Message });
    }
}).AddEndpointFilter<ValidationFilter<RegistrarRequest>>();

app.MapPost("/login", async (LoginRequest req, AuthService auth, CancellationToken ct) =>
{
    try
    {
        return Results.Ok(await auth.LoginAsync(req.Email, req.Senha, ct));
    }
    catch (CredenciaisInvalidasException)
    {
        return Results.Unauthorized();
    }
}).AddEndpointFilter<ValidationFilter<LoginRequest>>();

app.MapPost("/login-google", async (LoginGoogleRequest req, AuthService auth, CancellationToken ct) =>
{
    try
    {
        return Results.Ok(await auth.LoginComGoogleAsync(req.IdToken, ct));
    }
    catch (TokenGoogleInvalidoException)
    {
        return Results.Unauthorized();
    }
}).AddEndpointFilter<ValidationFilter<LoginGoogleRequest>>();

app.MapPost("/refresh", async (RenovarTokenRequest req, RefreshTokenService refreshTokens, CancellationToken ct) =>
{
    try
    {
        var tokens = await refreshTokens.RenovarAsync(req.RefreshToken, ct);
        return Results.Ok(new RenovarTokenResponse(tokens.AccessToken, tokens.RefreshToken));
    }
    catch (Exception ex) when (ex is RefreshTokenInvalidoException or RefreshTokenReutilizadoException)
    {
        return Results.Unauthorized();
    }
}).AddEndpointFilter<ValidationFilter<RenovarTokenRequest>>();

app.MapPost("/logout", async (RenovarTokenRequest req, RefreshTokenService refreshTokens, CancellationToken ct) =>
{
    await refreshTokens.RevogarAsync(req.RefreshToken, ct);
    return Results.NoContent();
}).AddEndpointFilter<ValidationFilter<RenovarTokenRequest>>();

app.MapGet("/me", async (ClaimsPrincipal principal, IUsuarioRepository repo, CancellationToken ct) =>
{
    var id = IdDoUsuario(principal);
    var usuario = await repo.ObterPorIdAsync(id, ct);
    return usuario is null
        ? Results.NotFound()
        : Results.Ok(new UsuarioResponse(usuario.Id, usuario.Nome, usuario.Email, usuario.CriadoEm, usuario.OnboardingConcluido));
}).RequireAuthorization();

app.MapPut("/perfil", async (AtualizarPerfilRequest req, ClaimsPrincipal principal, AuthService auth, CancellationToken ct) =>
{
    try
    {
        return Results.Ok(await auth.AtualizarPerfilAsync(IdDoUsuario(principal), req.Nome, ct));
    }
    catch (UsuarioNaoEncontradoException)
    {
        return Results.NotFound();
    }
}).RequireAuthorization().AddEndpointFilter<ValidationFilter<AtualizarPerfilRequest>>();

app.MapPut("/senha", async (TrocarSenhaRequest req, ClaimsPrincipal principal, AuthService auth, CancellationToken ct) =>
{
    try
    {
        await auth.TrocarSenhaAsync(IdDoUsuario(principal), req.SenhaAtual, req.NovaSenha, ct);
        return Results.NoContent();
    }
    catch (SenhaAtualIncorretaException ex)
    {
        return Results.BadRequest(new { erro = ex.Message });
    }
    catch (UsuarioNaoEncontradoException)
    {
        return Results.NotFound();
    }
}).RequireAuthorization().AddEndpointFilter<ValidationFilter<TrocarSenhaRequest>>();

app.MapPut("/perfil-onboarding", async (PerfilOnboardingRequest req, ClaimsPrincipal principal, AuthService auth, CancellationToken ct) =>
{
    try
    {
        return Results.Ok(await auth.DefinirPerfilOnboardingAsync(IdDoUsuario(principal), req, ct));
    }
    catch (UsuarioNaoEncontradoException)
    {
        return Results.NotFound();
    }
}).RequireAuthorization().AddEndpointFilter<ValidationFilter<PerfilOnboardingRequest>>();

app.MapPost("/perfil-onboarding/pular", async (ClaimsPrincipal principal, AuthService auth, CancellationToken ct) =>
{
    try
    {
        return Results.Ok(await auth.PularOnboardingAsync(IdDoUsuario(principal), ct));
    }
    catch (UsuarioNaoEncontradoException)
    {
        return Results.NotFound();
    }
}).RequireAuthorization();

app.MapHealthChecks("/health");

app.Run();

static Guid IdDoUsuario(ClaimsPrincipal principal) =>
    Guid.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

public partial class Program { } // visivel para os testes de integracao (WebApplicationFactory)
