using Gamificacao.Api.Aplicacao;
using Gamificacao.Api.Contratos;
using Gamificacao.Api.Mensageria;
using Gamificacao.Api.Persistencia;
using Gamificacao.Api.Regras;
using Microsoft.EntityFrameworkCore;

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

builder.Services.AddHealthChecks()
    .AddDbContextCheck<GamificacaoDbContext>("postgres");

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

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

app.MapHealthChecks("/health");

app.Run();

public partial class Program { } // visivel para os testes de integracao (WebApplicationFactory)
