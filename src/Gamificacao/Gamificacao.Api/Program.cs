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

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddHostedService<LancamentoConsumerService>();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/saldo", async (IMovimentoMoedasRepository repo, CancellationToken ct) =>
    Results.Ok(new { Saldo = await repo.ObterSaldoAsync(ct) }));

app.Run();
