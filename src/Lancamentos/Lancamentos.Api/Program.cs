using FluentValidation;
using Lancamentos.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Lancamentos.Application.Repositorios;
using Lancamentos.Infrastructure.Repositorios;
using Lancamentos.Api.Contratos;
using Lancamentos.Api.Validacao;
using Lancamentos.Domain.Entidades;
using Lancamentos.Application.Relatorios;
using Lancamentos.Infrastructure.Mensageria;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LancamentosDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("LancamentosDb")));

builder.Services.AddScoped<ILancamentoRepository, LancamentoRepository>();
builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();
builder.Services.AddScoped<IOrcamentoRepository, OrcamentoRepository>();
builder.Services.AddScoped<IRelatorioRepository, RelatorioRepository>();
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddSingleton<RabbitMqConnection>();
builder.Services.AddHostedService<OutboxPublisherService>();

// Validators são stateless — singleton evita recriar a cada request.
builder.Services.AddSingleton<IValidator<CriarLancamentoRequest>, CriarLancamentoRequestValidator>();
builder.Services.AddSingleton<IValidator<AtualizarLancamentoRequest>, AtualizarLancamentoRequestValidator>();
builder.Services.AddSingleton<IValidator<CriarCategoriaRequest>, CriarCategoriaRequestValidator>();
builder.Services.AddSingleton<IValidator<DefinirOrcamentoRequest>, DefinirOrcamentoRequestValidator>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<LancamentosDbContext>();

builder.Services.AddOpenApi();

var app = builder.Build();

// ----- Lançamentos -----

app.MapPost("/lancamentos", async (CriarLancamentoRequest req, ILancamentoRepository repo, CancellationToken ct) =>
{
    var lancamento = new Lancamento(req.Descricao, req.Valor, req.Tipo, req.CategoriaId, req.Data);
    await repo.AdicionarAsync(lancamento, ct);
    return Results.Created($"/lancamentos/{lancamento.Id}", ParaResponse(lancamento));
}).AddEndpointFilter<ValidationFilter<CriarLancamentoRequest>>();

app.MapGet("/lancamentos/{id:guid}", async (Guid id, ILancamentoRepository repo, CancellationToken ct) =>
{
    var l = await repo.ObterPorIdAsync(id, ct);
    return l is null ? Results.NotFound() : Results.Ok(ParaResponse(l));
});

app.MapGet("/lancamentos", async (DateTime inicio, DateTime fim, ILancamentoRepository repo, CancellationToken ct) =>
{
    var lista = await repo.ListarPorPeriodoAsync(inicio, fim, ct);
    return Results.Ok(lista.Select(ParaResponse));
});

app.MapPut("/lancamentos/{id:guid}", async (Guid id, AtualizarLancamentoRequest req, ILancamentoRepository repo, CancellationToken ct) =>
{
    var lancamento = await repo.ObterPorIdAsync(id, ct);
    if (lancamento is null)
        return Results.NotFound();

    lancamento.Atualizar(req.Descricao, req.Valor, req.Tipo, req.CategoriaId, req.Data);
    await repo.AtualizarAsync(lancamento, ct);
    return Results.Ok(ParaResponse(lancamento));
}).AddEndpointFilter<ValidationFilter<AtualizarLancamentoRequest>>();

app.MapDelete("/lancamentos/{id:guid}", async (Guid id, ILancamentoRepository repo, CancellationToken ct) =>
    await repo.RemoverAsync(id, ct) ? Results.NoContent() : Results.NotFound());

// ----- Categorias -----

app.MapGet("/categorias", async (ICategoriaRepository repo, CancellationToken ct) =>
{
    var categorias = await repo.ListarAsync(ct);
    return Results.Ok(categorias.Select(c => new CategoriaResponse(c.Id, c.Nome)));
});

app.MapPost("/categorias", async (CriarCategoriaRequest req, ICategoriaRepository repo, CancellationToken ct) =>
{
    if (await repo.ExisteComNomeAsync(req.Nome, ct))
        return Results.Conflict(new { erro = $"Categoria '{req.Nome.Trim()}' já existe." });

    var categoria = new Categoria(req.Nome);
    await repo.AdicionarAsync(categoria, ct);
    return Results.Created($"/categorias/{categoria.Id}", new CategoriaResponse(categoria.Id, categoria.Nome));
}).AddEndpointFilter<ValidationFilter<CriarCategoriaRequest>>();

// ----- Orçamentos (teto de gasto mensal por categoria, estilo Mobills) -----

app.MapGet("/orcamentos", async (IOrcamentoRepository orcamentos, ICategoriaRepository categorias, IRelatorioRepository relatorios, CancellationToken ct) =>
{
    var hoje = DateTime.Today;
    var inicio = new DateTime(hoje.Year, hoje.Month, 1);
    var fim = inicio.AddMonths(1).AddDays(-1);

    var lista = await orcamentos.ListarAsync(ct);
    var nomes = (await categorias.ListarAsync(ct)).ToDictionary(c => c.Id, c => c.Nome);
    var gastos = (await relatorios.GastosPorCategoriaAsync(inicio, fim, ct)).ToDictionary(g => g.CategoriaId, g => g.TotalGasto);

    var status = lista.Select(o =>
    {
        var gasto = gastos.GetValueOrDefault(o.CategoriaId);
        return new OrcamentoStatusResponse(
            o.CategoriaId,
            nomes.GetValueOrDefault(o.CategoriaId, "?"),
            o.ValorLimite,
            gasto,
            Math.Round(gasto / o.ValorLimite * 100, 1));
    });

    return Results.Ok(status);
});

app.MapPut("/orcamentos", async (DefinirOrcamentoRequest req, IOrcamentoRepository orcamentos, ICategoriaRepository categorias, CancellationToken ct) =>
{
    if (await categorias.ObterPorIdAsync(req.CategoriaId, ct) is null)
        return Results.NotFound(new { erro = "Categoria não encontrada." });

    var existente = await orcamentos.ObterPorCategoriaAsync(req.CategoriaId, ct);
    if (existente is null)
    {
        await orcamentos.AdicionarAsync(new Orcamento(req.CategoriaId, req.ValorLimite), ct);
    }
    else
    {
        existente.AlterarLimite(req.ValorLimite);
        await orcamentos.AtualizarAsync(existente, ct);
    }

    return Results.NoContent();
}).AddEndpointFilter<ValidationFilter<DefinirOrcamentoRequest>>();

app.MapDelete("/orcamentos/{categoriaId:guid}", async (Guid categoriaId, IOrcamentoRepository orcamentos, CancellationToken ct) =>
{
    var existente = await orcamentos.ObterPorCategoriaAsync(categoriaId, ct);
    if (existente is null)
        return Results.NotFound();

    await orcamentos.RemoverAsync(existente.Id, ct);
    return Results.NoContent();
});

// ----- Relatórios (procedures/views nativas no SQL Server) -----

app.MapGet("/relatorios/gastos-por-categoria", async (DateTime inicio, DateTime fim, IRelatorioRepository repo, CancellationToken ct) =>
    Results.Ok(await repo.GastosPorCategoriaAsync(inicio, fim, ct)));

app.MapGet("/relatorios/saldo", async (DateTime inicio, DateTime fim, IRelatorioRepository repo, CancellationToken ct) =>
    Results.Ok(new { Saldo = await repo.SaldoPeriodoAsync(inicio, fim, ct) }));

app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();

static LancamentoResponse ParaResponse(Lancamento l) =>
    new(l.Id, l.Descricao, l.Valor, l.Tipo, l.CategoriaId, l.Data);
