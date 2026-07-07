using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

builder.Services.AddHealthChecks()
    .AddDbContextCheck<NotificacoesDbContext>("postgres");

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/notificacoes", async (ClaimsPrincipal principal, INotificacaoRepository repo, CancellationToken ct) =>
{
    var notificacoes = await repo.ListarAsync(IdDoUsuario(principal), ct);
    return Results.Ok(notificacoes.Select(n => new NotificacaoResponse(n.Id, n.Tipo, n.Mensagem, n.Lida, n.CriadoEm)));
});

app.MapPost("/notificacoes/{id:guid}/marcar-lida", async (Guid id, ClaimsPrincipal principal, INotificacaoRepository repo, CancellationToken ct) =>
{
    var encontrada = await repo.MarcarComoLidaAsync(id, IdDoUsuario(principal), ct);
    return encontrada ? Results.NoContent() : Results.NotFound();
});

app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

static Guid IdDoUsuario(ClaimsPrincipal principal) =>
    Guid.Parse(principal.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)!);

public partial class Program { } // visivel para os testes de integracao (WebApplicationFactory)
