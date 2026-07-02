using Lancamentos.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Lancamentos.Application.Repositorios;
using Lancamentos.Infrastructure.Repositorios;
using Lancamentos.Api.Contratos;
using Lancamentos.Domain.Entidades;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LancamentosDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("LancamentosDb")));

builder.Services.AddScoped<ILancamentoRepository, LancamentoRepository>();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapPost("/lancamentos", async (CriarLancamentoRequest req, ILancamentoRepository repo, CancellationToken ct) =>
{
    var lancamento = new Lancamento(req.Descricao, req.Valor, req.Tipo, req.CategoriaId, req.Data);
    await repo.AdicionarAsync(lancamento, ct);
    return Results.Created($"/lancamentos/{lancamento.Id}",
        new LancamentoResponse(lancamento.Id, lancamento.Descricao, lancamento.Valor, lancamento.Tipo, lancamento.CategoriaId, lancamento.Data));
});

app.MapGet("/lancamentos/{id:guid}", async (Guid id, ILancamentoRepository repo, CancellationToken ct) =>
{
    var l = await repo.ObterPorIdAsync(id, ct);
    return l is null
        ? Results.NotFound()
        : Results.Ok(new LancamentoResponse(l.Id, l.Descricao, l.Valor, l.Tipo, l.CategoriaId, l.Data));
});

app.MapGet("/lancamentos", async (DateTime inicio, DateTime fim, ILancamentoRepository repo, CancellationToken ct) =>
{
    var lista = await repo.ListarPorPeriodoAsync(inicio, fim, ct);
    return Results.Ok(lista.Select(l => new LancamentoResponse(l.Id, l.Descricao, l.Valor, l.Tipo, l.CategoriaId, l.Data)));
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
