using Lancamentos.Application.Importacao;
using Lancamentos.Domain.Entidades;
using Lancamentos.Infrastructure.Importacao;
using Lancamentos.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Tests.Repositorios;

/// <summary>
/// Adapters do modo "Banco" da importação (Importacoes:Modo, usados no deploy,
/// onde não há LocalStack/AWS): armazenamento do CSV numa tabela e fila via
/// polling das importações Pendentes. Mesmas portas do modo Aws.
/// </summary>
public class ImportacaoBancoAdaptersTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;

    public ImportacaoBancoAdaptersTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    private LancamentosDbContext CriarDbContext()
    {
        var options = new DbContextOptionsBuilder<LancamentosDbContext>()
            .UseSqlServer(_fixture.ConnectionString)
            .Options;
        return new LancamentosDbContext(options);
    }

    [Fact]
    public async Task ArmazenamentoBanco_SalvarEBaixar_DevolveOMesmoConteudo()
    {
        var chave = $"extratos/{Guid.NewGuid()}.csv";
        const string conteudo = "Data;Descricao;Valor;Tipo;Categoria\n01/06/2026;Salário;3.500,00;Receita;Salário";

        await using (var db = CriarDbContext())
        {
            await new ArmazenamentoExtratoBanco(db).SalvarAsync(chave, conteudo, CancellationToken.None);
        }

        await using var dbLeitura = CriarDbContext();
        var baixado = await new ArmazenamentoExtratoBanco(dbLeitura).BaixarAsync(chave, CancellationToken.None);

        Assert.Equal(conteudo, baixado);
    }

    [Fact]
    public async Task ArmazenamentoBanco_BaixarChaveInexistente_DeveLancar()
    {
        await using var db = CriarDbContext();
        var armazenamento = new ArmazenamentoExtratoBanco(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => armazenamento.BaixarAsync($"extratos/{Guid.NewGuid()}.csv", CancellationToken.None));
    }

    [Fact]
    public async Task FilaBanco_ImportacaoPendente_ApareceNoReceber()
    {
        var importacao = new ImportacaoExtrato("extrato.csv", Guid.NewGuid());
        await using (var db = CriarDbContext())
        {
            db.Importacoes.Add(importacao);
            await db.SaveChangesAsync();
        }

        await using var dbFila = CriarDbContext();
        var mensagens = await new FilaImportacoesBanco(dbFila).ReceberAsync(CancellationToken.None);

        Assert.Contains(mensagens, m => m.ImportacaoId == importacao.Id);
    }

    [Fact]
    public async Task FilaBanco_ImportacaoQueSaiuDePendente_NaoApareceNoReceber()
    {
        var importacao = new ImportacaoExtrato("extrato.csv", Guid.NewGuid());
        importacao.IniciarProcessamento();
        await using (var db = CriarDbContext())
        {
            db.Importacoes.Add(importacao);
            await db.SaveChangesAsync();
        }

        await using var dbFila = CriarDbContext();
        var mensagens = await new FilaImportacoesBanco(dbFila).ReceberAsync(CancellationToken.None);

        Assert.DoesNotContain(mensagens, m => m.ImportacaoId == importacao.Id);
    }

    [Fact]
    public async Task FilaBanco_EnfileirarERemover_SaoNoOpsSemErro()
    {
        // O contrato exige os métodos (o publicador da outbox e o worker os
        // chamam), mas neste modo o INSERT e a transição de status já fazem
        // esses papéis - aqui só garante que chamar não explode.
        await using var db = CriarDbContext();
        var fila = new FilaImportacoesBanco(db);

        await fila.EnfileirarAsync(Guid.NewGuid(), CancellationToken.None);
        await fila.RemoverAsync(new MensagemImportacao(Guid.NewGuid(), string.Empty), CancellationToken.None);
        await fila.GarantirInfraestruturaAsync(CancellationToken.None);
    }
}
