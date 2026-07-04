using Amazon.Runtime;
using Amazon.S3;
using Amazon.SQS;
using FluentValidation;
using Lancamentos.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Lancamentos.Application.Importacao;
using Lancamentos.Application.Repositorios;
using Lancamentos.Infrastructure.Aws;
using Lancamentos.Infrastructure.Repositorios;
using Lancamentos.Api.Contratos;
using Lancamentos.Api.Validacao;
using Lancamentos.Domain.Entidades;
using Lancamentos.Application.Relatorios;
using Lancamentos.Infrastructure.Mensageria;
using Lancamentos.Infrastructure.Recorrencias;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LancamentosDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("LancamentosDb")));

builder.Services.AddScoped<ILancamentoRepository, LancamentoRepository>();
builder.Services.AddScoped<IContaRepository, ContaRepository>();
builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();
builder.Services.AddScoped<IOrcamentoRepository, OrcamentoRepository>();
builder.Services.AddScoped<IRecorrenciaRepository, RecorrenciaRepository>();
builder.Services.AddScoped<IObjetivoRepository, ObjetivoRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddHostedService<RecorrenciaWorker>();
builder.Services.AddScoped<IRelatorioRepository, RelatorioRepository>();
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddSingleton<RabbitMqConnection>();
builder.Services.AddHostedService<OutboxPublisherService>();

// AWS via LocalStack: SDK oficial apontando pro endpoint local (custo zero).
// Em produção bastaria remover ServiceUrl da config — o SDK resolve os endpoints reais.
builder.Services.Configure<AwsOptions>(builder.Configuration.GetSection(AwsOptions.SectionName));
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var aws = sp.GetRequiredService<IOptions<AwsOptions>>().Value;
    return new AmazonS3Client(
        new BasicAWSCredentials(aws.AccessKey, aws.SecretKey),
        new AmazonS3Config
        {
            ServiceURL = aws.ServiceUrl,
            AuthenticationRegion = aws.Region,
            ForcePathStyle = true // LocalStack não resolve bucket como subdomínio
        });
});
builder.Services.AddSingleton<IAmazonSQS>(sp =>
{
    var aws = sp.GetRequiredService<IOptions<AwsOptions>>().Value;
    return new AmazonSQSClient(
        new BasicAWSCredentials(aws.AccessKey, aws.SecretKey),
        new AmazonSQSConfig
        {
            ServiceURL = aws.ServiceUrl,
            AuthenticationRegion = aws.Region
        });
});
builder.Services.AddScoped<IArmazenamentoExtrato, ArmazenamentoExtratoS3>();
builder.Services.AddScoped<IFilaImportacoes, FilaImportacoesSqs>();
builder.Services.AddScoped<IImportacaoRepository, ImportacaoRepository>();
builder.Services.AddHostedService<ImportacaoExtratoWorker>();

// Validators são stateless — singleton evita recriar a cada request.
builder.Services.AddSingleton<IValidator<CriarLancamentoRequest>, CriarLancamentoRequestValidator>();
builder.Services.AddSingleton<IValidator<AtualizarLancamentoRequest>, AtualizarLancamentoRequestValidator>();
builder.Services.AddSingleton<IValidator<CriarCategoriaRequest>, CriarCategoriaRequestValidator>();
builder.Services.AddSingleton<IValidator<DefinirOrcamentoRequest>, DefinirOrcamentoRequestValidator>();
builder.Services.AddSingleton<IValidator<CriarContaRequest>, CriarContaRequestValidator>();
builder.Services.AddSingleton<IValidator<CriarRecorrenciaRequest>, CriarRecorrenciaRequestValidator>();
builder.Services.AddSingleton<IValidator<CriarObjetivoRequest>, CriarObjetivoRequestValidator>();
builder.Services.AddSingleton<IValidator<AporteRequest>, AporteRequestValidator>();
builder.Services.AddSingleton<IValidator<TransferenciaRequest>, TransferenciaRequestValidator>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<LancamentosDbContext>();

builder.Services.AddOpenApi();

var app = builder.Build();

// ----- Lançamentos -----

app.MapPost("/lancamentos", async (CriarLancamentoRequest req, ILancamentoRepository repo, IContaRepository contas, ITagRepository tags, CancellationToken ct) =>
{
    if (await contas.ObterPorIdAsync(req.ContaId, ct) is null)
        return Results.BadRequest(new { erro = "Conta não encontrada." });

    var lancamento = new Lancamento(req.Descricao, req.Valor, req.Tipo, req.CategoriaId, req.ContaId, req.Data);
    if (req.Tags is { Count: > 0 })
        lancamento.DefinirTags(await tags.ObterOuCriarAsync(req.Tags, ct));

    await repo.AdicionarAsync(lancamento, ct);
    return Results.Created($"/lancamentos/{lancamento.Id}", ParaResponse(lancamento));
}).AddEndpointFilter<ValidationFilter<CriarLancamentoRequest>>();

app.MapGet("/lancamentos/{id:guid}", async (Guid id, ILancamentoRepository repo, CancellationToken ct) =>
{
    var l = await repo.ObterPorIdAsync(id, ct);
    return l is null ? Results.NotFound() : Results.Ok(ParaResponse(l));
});

app.MapGet("/lancamentos", async (
    DateTime inicio,
    DateTime fim,
    Guid? categoriaId,
    Guid? contaId,
    TipoLancamento? tipo,
    string? texto,
    string? tags,
    int? skip,
    int? take,
    ILancamentoRepository repo,
    CancellationToken ct) =>
{
    var filtro = new FiltroLancamentos(
        inicio, fim, categoriaId, contaId, tipo, texto,
        Tags: tags?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        Skip: skip ?? 0,
        Take: take ?? 50);

    var pagina = await repo.ListarAsync(filtro, ct);
    return Results.Ok(new PaginaLancamentosResponse(pagina.Total, pagina.Itens.Select(ParaResponse).ToList()));
});

app.MapPut("/lancamentos/{id:guid}", async (Guid id, AtualizarLancamentoRequest req, ILancamentoRepository repo, IContaRepository contas, ITagRepository tags, CancellationToken ct) =>
{
    var lancamento = await repo.ObterPorIdAsync(id, ct);
    if (lancamento is null)
        return Results.NotFound();

    if (await contas.ObterPorIdAsync(req.ContaId, ct) is null)
        return Results.BadRequest(new { erro = "Conta não encontrada." });

    lancamento.Atualizar(req.Descricao, req.Valor, req.Tipo, req.CategoriaId, req.ContaId, req.Data);
    if (req.Tags is not null)
        lancamento.DefinirTags(await tags.ObterOuCriarAsync(req.Tags, ct));

    await repo.AtualizarAsync(lancamento, ct);
    return Results.Ok(ParaResponse(lancamento));
}).AddEndpointFilter<ValidationFilter<AtualizarLancamentoRequest>>();

app.MapDelete("/lancamentos/{id:guid}", async (Guid id, ILancamentoRepository repo, CancellationToken ct) =>
    await repo.RemoverAsync(id, ct) ? Results.NoContent() : Results.NotFound());

// ----- Tags (etiquetas livres) -----

app.MapGet("/tags", async (ITagRepository repo, CancellationToken ct) =>
{
    var lista = await repo.ListarAsync(ct);
    return Results.Ok(lista.Select(t => new TagResponse(t.Id, t.Nome)));
});

// ----- Contas (caixas de dinheiro, estilo Mobills) -----

app.MapGet("/contas", async (IContaRepository repo, CancellationToken ct) =>
{
    var contas = await repo.ListarAsync(ct);
    return Results.Ok(contas.Select(c => new ContaResponse(c.Id, c.Nome)));
});

app.MapPost("/contas", async (CriarContaRequest req, IContaRepository repo, CancellationToken ct) =>
{
    if (await repo.ExisteComNomeAsync(req.Nome, ct))
        return Results.Conflict(new { erro = $"Conta '{req.Nome.Trim()}' já existe." });

    var conta = new Conta(req.Nome);
    await repo.AdicionarAsync(conta, ct);
    return Results.Created($"/contas/{conta.Id}", new ContaResponse(conta.Id, conta.Nome));
}).AddEndpointFilter<ValidationFilter<CriarContaRequest>>();

// saldo por conta via view SQL nativa (vw_SaldoPorConta) — requisito de SQL da vaga
app.MapGet("/contas/saldos", async (IRelatorioRepository relatorios, CancellationToken ct) =>
{
    var saldos = await relatorios.SaldosPorContaAsync(ct);
    return Results.Ok(saldos.Select(s => new SaldoPorContaResponse(s.ContaId, s.Conta, s.Saldo)));
});

// ----- Transferências entre contas -----

app.MapPost("/transferencias", async (TransferenciaRequest req, ILancamentoRepository lancamentos, IContaRepository contas, CancellationToken ct) =>
{
    var origem = await contas.ObterPorIdAsync(req.ContaOrigemId, ct);
    var destino = await contas.ObterPorIdAsync(req.ContaDestinoId, ct);
    if (origem is null || destino is null)
        return Results.BadRequest(new { erro = "Conta de origem ou destino não encontrada." });

    var saida = new Lancamento($"Transferência para {destino.Nome}", req.Valor,
        TipoLancamento.Despesa, Categoria.TransferenciaId, origem.Id, DateTime.UtcNow);
    var entrada = new Lancamento($"Transferência de {origem.Nome}", req.Valor,
        TipoLancamento.Receita, Categoria.TransferenciaId, destino.Id, DateTime.UtcNow);

    // os dois lançamentos na MESMA transação local (mesmo banco) — não precisa
    // de Saga: atomicidade aqui é do próprio SQL Server, diferente do resgate
    // de moedas que cruza dois serviços/bancos
    await lancamentos.AdicionarTransferenciaAsync(saida, entrada, ct);

    return Results.Created("/transferencias", new TransferenciaResponse(saida.Id, entrada.Id));
}).AddEndpointFilter<ValidationFilter<TransferenciaRequest>>();

// ----- Recorrências (contas fixas, estilo Mobills) -----

app.MapGet("/recorrencias", async (IRecorrenciaRepository repo, CancellationToken ct) =>
{
    var lista = await repo.ListarAsync(ct);
    return Results.Ok(lista.Select(ParaRecorrenciaResponse));
});

app.MapPost("/recorrencias", async (CriarRecorrenciaRequest req, IRecorrenciaRepository repo, IContaRepository contas, CancellationToken ct) =>
{
    if (await contas.ObterPorIdAsync(req.ContaId, ct) is null)
        return Results.BadRequest(new { erro = "Conta não encontrada." });

    var recorrencia = new LancamentoRecorrente(req.Descricao, req.Valor, req.Tipo, req.CategoriaId, req.ContaId, req.DiaDoMes);
    await repo.AdicionarAsync(recorrencia, ct);
    return Results.Created($"/recorrencias/{recorrencia.Id}", ParaRecorrenciaResponse(recorrencia));
}).AddEndpointFilter<ValidationFilter<CriarRecorrenciaRequest>>();

app.MapPost("/recorrencias/{id:guid}/pausar", async (Guid id, IRecorrenciaRepository repo, CancellationToken ct) =>
{
    var recorrencia = await repo.ObterPorIdAsync(id, ct);
    if (recorrencia is null) return Results.NotFound();

    recorrencia.Pausar();
    await repo.AtualizarAsync(recorrencia, ct);
    return Results.Ok(ParaRecorrenciaResponse(recorrencia));
});

app.MapPost("/recorrencias/{id:guid}/reativar", async (Guid id, IRecorrenciaRepository repo, CancellationToken ct) =>
{
    var recorrencia = await repo.ObterPorIdAsync(id, ct);
    if (recorrencia is null) return Results.NotFound();

    recorrencia.Reativar();
    await repo.AtualizarAsync(recorrencia, ct);
    return Results.Ok(ParaRecorrenciaResponse(recorrencia));
});

// ----- Objetivos (metas de poupanca, estilo Mobills) -----

app.MapGet("/objetivos", async (IObjetivoRepository repo, CancellationToken ct) =>
{
    var lista = await repo.ListarAsync(ct);
    return Results.Ok(lista.Select(ParaObjetivoResponse));
});

app.MapPost("/objetivos", async (CriarObjetivoRequest req, IObjetivoRepository repo, CancellationToken ct) =>
{
    var objetivo = new Objetivo(req.Nome, req.ValorAlvo, req.DataAlvo);
    await repo.AdicionarAsync(objetivo, ct);
    return Results.Created($"/objetivos/{objetivo.Id}", ParaObjetivoResponse(objetivo));
}).AddEndpointFilter<ValidationFilter<CriarObjetivoRequest>>();

app.MapPost("/objetivos/{id:guid}/aportes", async (Guid id, AporteRequest req, IObjetivoRepository repo, IContaRepository contas, CancellationToken ct) =>
{
    var objetivo = await repo.ObterPorIdAsync(id, ct);
    if (objetivo is null)
        return Results.NotFound();
    if (objetivo.Concluido)
        return Results.Conflict(new { erro = $"Objetivo '{objetivo.Nome}' ja foi concluido." });
    if (await contas.ObterPorIdAsync(req.ContaId, ct) is null)
        return Results.BadRequest(new { erro = "Conta nao encontrada." });

    var concluiu = objetivo.Aportar(req.Valor);

    // o aporte vira uma despesa real na conta escolhida (o dinheiro "sai" para a reserva)
    var lancamento = new Lancamento(
        $"Aporte: {objetivo.Nome}", req.Valor, TipoLancamento.Despesa,
        Categoria.ObjetivosId, req.ContaId, DateTime.UtcNow);

    await repo.RegistrarAporteAsync(objetivo, lancamento, concluiu, ct);
    return Results.Ok(ParaObjetivoResponse(objetivo));
}).AddEndpointFilter<ValidationFilter<AporteRequest>>();

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

// ----- Importação de extrato CSV (assíncrona via S3 + SQS/LocalStack) -----

app.MapPost("/importacoes", async (
    HttpRequest request,
    IImportacaoRepository importacoes,
    IArmazenamentoExtrato armazenamento,
    IFilaImportacoes fila,
    CancellationToken ct) =>
{
    using var leitor = new StreamReader(request.Body);
    var conteudo = await leitor.ReadToEndAsync(ct);

    if (string.IsNullOrWhiteSpace(conteudo))
        return Results.BadRequest(new { erro = "Envie o conteúdo do extrato CSV no corpo da requisição." });
    if (conteudo.Length > 1_000_000)
        return Results.BadRequest(new { erro = "Arquivo acima do limite de 1 MB." });

    var nomeArquivo = request.Query.TryGetValue("nomeArquivo", out var nome) && !string.IsNullOrWhiteSpace(nome)
        ? nome.ToString()
        : "extrato.csv";

    var importacao = new ImportacaoExtrato(nomeArquivo);
    await armazenamento.SalvarAsync(importacao.ChaveS3, conteudo, ct); // 1. arquivo no S3
    await importacoes.AdicionarAsync(importacao, ct);                  // 2. rastreio no banco
    await fila.EnfileirarAsync(importacao.Id, ct);                     // 3. trabalho na fila

    // 202 Accepted + Location: async request-reply — o cliente acompanha por polling
    return Results.Accepted($"/importacoes/{importacao.Id}", ParaImportacaoResponse(importacao));
});

app.MapGet("/importacoes/{id:guid}", async (Guid id, IImportacaoRepository importacoes, CancellationToken ct) =>
{
    var importacao = await importacoes.ObterPorIdAsync(id, ct);
    return importacao is null ? Results.NotFound() : Results.Ok(ParaImportacaoResponse(importacao));
});

// ----- Relatórios (procedures/views nativas no SQL Server) -----

app.MapGet("/relatorios/gastos-por-categoria", async (DateTime inicio, DateTime fim, IRelatorioRepository repo, CancellationToken ct) =>
    Results.Ok(await repo.GastosPorCategoriaAsync(inicio, fim, ct)));

app.MapGet("/relatorios/saldo", async (DateTime inicio, DateTime fim, IRelatorioRepository repo, CancellationToken ct) =>
    Results.Ok(new { Saldo = await repo.SaldoPeriodoAsync(inicio, fim, ct) }));

app.MapGet("/relatorios/gastos-por-tag", async (DateTime inicio, DateTime fim, IRelatorioRepository repo, CancellationToken ct) =>
    Results.Ok(await repo.GastosPorTagAsync(inicio, fim, ct)));

app.MapGet("/relatorios/evolucao-mensal", async (int? meses, IRelatorioRepository repo, CancellationToken ct) =>
{
    var quantidade = Math.Clamp(meses ?? 6, 1, 24);
    return Results.Ok(await repo.EvolucaoMensalAsync(quantidade, ct));
});

app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();

static LancamentoResponse ParaResponse(Lancamento l) =>
    new(l.Id, l.Descricao, l.Valor, l.Tipo, l.CategoriaId, l.ContaId, l.Data, l.RecorrenciaId,
        l.Tags.Select(t => t.Nome).OrderBy(n => n).ToList());

static RecorrenciaResponse ParaRecorrenciaResponse(LancamentoRecorrente r) =>
    new(r.Id, r.Descricao, r.Valor, r.Tipo, r.CategoriaId, r.ContaId, r.DiaDoMes, r.Ativa);

static ObjetivoResponse ParaObjetivoResponse(Objetivo o) =>
    new(o.Id, o.Nome, o.ValorAlvo, o.DataAlvo, o.ValorAcumulado,
        o.PercentualConcluido, o.ValorMensalNecessario(DateTime.Today), o.Concluido);

static ImportacaoStatusResponse ParaImportacaoResponse(ImportacaoExtrato i) =>
    new(i.Id, i.NomeArquivo, i.Status.ToString(), i.LinhasImportadas, i.LinhasComErro, i.Erro, i.CriadoEm, i.ProcessadoEm);
