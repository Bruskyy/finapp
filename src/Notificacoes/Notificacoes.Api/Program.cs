using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Notificacoes.Api.Aplicacao;
using Notificacoes.Api.Contratos;
using Notificacoes.Api.Mensageria;
using Notificacoes.Api.Persistencia;
using Notificacoes.Api.Provedores;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<NotificacoesDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("NotificacoesDb")));

builder.Services.AddScoped<INotificacaoRepository, NotificacaoRepository>();

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddSingleton<RabbitMqConnection>();
builder.Services.AddSingleton<INotificacaoProvider, NotificacaoProviderSimulado>();
builder.Services.AddHostedService<ResgateSolicitadoConsumerService>();
builder.Services.AddHostedService<LancamentoCriadoConsumerService>();
builder.Services.AddHostedService<ApoioSolicitadoConsumerService>();

// Push real (Roadmap 1.0, Sprint 5) - AddHttpClient dá o HttpClient pooled
// (IHttpClientFactory) que ProvedorPushExpo usa pra falar com a Expo Push API.
builder.Services.AddScoped<IDispositivoPushRepository, DispositivoPushRepository>();
builder.Services.AddScoped<NotificacaoPushService>();
builder.Services.AddHttpClient<IProvedorPush, ProvedorPushExpo>();

// Valida o Bearer token de novo aqui (mesma config do Gateway/Usuarios.Api)
// em vez de confiar cegamente em quem chamou - zero trust real, mesmo padrão
// já usado em Lancamentos.Api/Gamificacao.Api (ver README, "Zero trust real").
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
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

// FallbackPolicy exige autenticação em qualquer endpoint por padrão -
// /health é a única exceção explícita (AllowAnonymous).
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Sem AddDbContextCheck de propósito - ver Lancamentos.Api/Program.cs pro
// porquê (keep-alive.yml + health check com banco esgotou a cota gratuita
// mensal do Azure SQL de outro serviço; mesmo padrão aqui evita repetir no
// Postgres/Neon deste).
builder.Services.AddHealthChecks();

builder.Services.AddOpenApi();

var app = builder.Build();

// Aplica migrations pendentes no boot - o deploy (Render) só sobe o
// código a cada push, não roda `dotnet ef database update` sozinho.
// Sem isso, um PR que inclua migration quebra produção em silêncio até
// alguém rodar a migration manualmente (ver README, "Migração automática
// no boot"). Migrate() é idempotente - não faz nada se já estiver em dia.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<NotificacoesDbContext>().Database.Migrate();
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

app.MapGet("/notificacoes", async (ClaimsPrincipal principal, INotificacaoRepository repo, CancellationToken ct) =>
{
    var notificacoes = await repo.ListarAsync(IdDoUsuario(principal), ct);
    return Results.Ok(notificacoes.Select(n => new NotificacaoResponse(
        n.Id, n.Tipo, n.Mensagem, n.Lida, n.CriadoEm,
        n.EconomiaVsSemanaAnterior, n.CategoriaMaiorGasto, n.ValorCategoriaMaiorGasto,
        n.DiasComLancamento, n.NomeObjetivoDestaque, n.PercentualObjetivoDestaque)));
});

app.MapPost("/notificacoes/{id:guid}/marcar-lida", async (Guid id, ClaimsPrincipal principal, INotificacaoRepository repo, CancellationToken ct) =>
{
    var encontrada = await repo.MarcarComoLidaAsync(id, IdDoUsuario(principal), ct);
    return encontrada ? Results.NoContent() : Results.NotFound();
});

app.MapPost("/dispositivos", async (RegistrarDispositivoRequest req, ClaimsPrincipal principal, IDispositivoPushRepository repo, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(req.Token))
        return Results.BadRequest(new { erro = "Token é obrigatório." });

    await repo.RegistrarAsync(IdDoUsuario(principal), req.Token, ct);
    return Results.NoContent();
});

// MapDelete não permite corpo inferido (só POST/PUT/PATCH permitem) - o
// RequestDelegateFactory lança em tempo de build de rota (derruba a
// aplicação inteira, não só este endpoint) sem o [FromBody] explícito.
app.MapDelete("/dispositivos", async ([FromBody] RegistrarDispositivoRequest req, ClaimsPrincipal principal, IDispositivoPushRepository repo, CancellationToken ct) =>
{
    await repo.RemoverAsync(IdDoUsuario(principal), req.Token, ct);
    return Results.NoContent();
});

app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

static Guid IdDoUsuario(ClaimsPrincipal principal) =>
    Guid.Parse(principal.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)!);

public partial class Program { } // visivel para os testes de integracao (WebApplicationFactory)
