using Lancamentos.Domain.Entidades;

namespace Lancamentos.Tests.Dominio;

public class ImportacaoExtratoTests
{
    [Fact]
    public void Criar_DeveIniciarPendenteComChaveS3DerivadaDoId()
    {
        var importacao = new ImportacaoExtrato("extrato-julho.csv", Guid.NewGuid());

        Assert.Equal(StatusImportacao.Pendente, importacao.Status);
        Assert.Equal("extrato-julho.csv", importacao.NomeArquivo);
        Assert.Equal($"extratos/{importacao.Id}.csv", importacao.ChaveS3);
        Assert.False(importacao.JaFoiProcessada);
    }

    [Fact]
    public void Criar_SemNomeArquivo_DeveLancarExcecao()
    {
        Assert.Throws<ArgumentException>(() => new ImportacaoExtrato("  ", Guid.NewGuid()));
    }

    [Fact]
    public void FluxoCompleto_PendenteProcessandoConcluida()
    {
        var importacao = new ImportacaoExtrato("extrato.csv", Guid.NewGuid());

        importacao.IniciarProcessamento();
        Assert.Equal(StatusImportacao.Processando, importacao.Status);

        importacao.Concluir(linhasImportadas: 10, linhasComErro: 2);
        Assert.Equal(StatusImportacao.Concluida, importacao.Status);
        Assert.Equal(10, importacao.LinhasImportadas);
        Assert.Equal(2, importacao.LinhasComErro);
        Assert.NotNull(importacao.ProcessadoEm);
        Assert.True(importacao.JaFoiProcessada);
    }

    [Fact]
    public void Falhar_DeveGuardarErro()
    {
        var importacao = new ImportacaoExtrato("extrato.csv", Guid.NewGuid());
        importacao.IniciarProcessamento();

        importacao.Falhar("Bucket indisponível.");

        Assert.Equal(StatusImportacao.Falhou, importacao.Status);
        Assert.Equal("Bucket indisponível.", importacao.Erro);
        Assert.True(importacao.JaFoiProcessada);
    }

    [Fact]
    public void IniciarProcessamento_QuandoJaProcessada_DeveLancarExcecao()
    {
        var importacao = new ImportacaoExtrato("extrato.csv", Guid.NewGuid());
        importacao.IniciarProcessamento();
        importacao.Concluir(1, 0);

        // é essa invariante que torna o consumo do SQS idempotente:
        // mensagem duplicada encontra a importação fora de Pendente e é descartada
        Assert.Throws<InvalidOperationException>(importacao.IniciarProcessamento);
    }

    [Fact]
    public void Concluir_SemIniciarProcessamento_DeveLancarExcecao()
    {
        var importacao = new ImportacaoExtrato("extrato.csv", Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => importacao.Concluir(1, 0));
    }
}
