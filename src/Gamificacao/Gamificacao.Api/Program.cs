using System.Security.Claims;
using System.Text;
using Gamificacao.Api.Aplicacao;
using Gamificacao.Api.Contratos;
using Gamificacao.Api.Mensageria;
using Gamificacao.Api.Persistencia;
using Gamificacao.Api.Regras;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<GamificacaoDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("GamificacaoDb")));

builder.Services.AddScoped<IMovimentoMoedasRepository, MovimentoMoedasRepository>();
builder.Services.AddScoped<IRegraPontuacao, RegraDespesaRegistrada>();
builder.Services.AddScoped<IRegraPontuacao, RegraReceitaRegistrada>();
builder.Services.AddScoped<CalculadoraDePontuacao>();
builder.Services.AddScoped<RegraObjetivoConcluido>();

builder.Services.AddScoped<IResgateRepository, ResgateRepository>();
builder.Services.AddScoped<ResgateService>();

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddSingleton<RabbitMqConnection>();
builder.Services.AddHostedService<LancamentoConsumerService>();
builder.Services.AddHostedService<OutboxPublisherService>();
builder.Services.AddHostedService<ResgateResultadoConsumerService>();

// Valida o Bearer token de novo aqui (mesma config do Gateway/Usuarios.Api)
// em vez de confiar cegamente em quem chamou - fecha a dívida técnica
// documentada no README (hoje, quem acessa esta porta direto sem passar
// pelo Gateway não encontra autenticação nenhuma).
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
    .AddDbContextCheck<GamificacaoDbContext>("postgres");

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/saldo", async (IMovimentoMoedasRepository repo, CancellationToken ct) =>
    Results.Ok(new { Saldo = await repo.ObterSaldoAsync(ct) }));

app.MapPost("/resgates", async (ResgateRequest req, ResgateService service, CancellationToken ct) =>
{
    try
    {
        var resgate = await service.SolicitarAsync(req.Quantidade, ct);
        return Results.Accepted($"/resgates/{resgate.Id}",
            new ResgateResponse(resgate.Id, resgate.Quantidade, resgate.Status.ToString()));
    }
    catch (SaldoInsuficienteException ex)
    {
        return Results.BadRequest(new { erro = ex.Message });
    }
});

app.MapGet("/resgates/{id:guid}", async (Guid id, IResgateRepository repo, CancellationToken ct) =>
{
    var resgate = await repo.ObterAsync(id, ct);
    return resgate is null
        ? Results.NotFound()
        : Results.Ok(new ResgateResponse(resgate.Id, resgate.Quantidade, resgate.Status.ToString()));
});

app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

static Guid IdDoUsuario(ClaimsPrincipal principal) =>
    Guid.Parse(principal.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)!);

public partial class Program { } // visivel para os testes de integracao (WebApplicationFactory)
