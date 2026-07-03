namespace Lancamentos.Domain.Entidades;

/// <summary>
/// Rastreia uma importação assíncrona de extrato CSV: o arquivo vai pro S3,
/// uma mensagem entra na fila SQS e um worker processa em background.
/// O cliente acompanha pelo status (202 Accepted + polling).
/// </summary>
public class ImportacaoExtrato
{
    public Guid Id { get; private set; }
    public string NomeArquivo { get; private set; }
    public StatusImportacao Status { get; private set; }
    public int LinhasImportadas { get; private set; }
    public int LinhasComErro { get; private set; }
    public string? Erro { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime? ProcessadoEm { get; private set; }

    private ImportacaoExtrato() { NomeArquivo = null!; }

    public ImportacaoExtrato(string nomeArquivo)
    {
        if (string.IsNullOrWhiteSpace(nomeArquivo))
            throw new ArgumentException("Nome do arquivo é obrigatório.", nameof(nomeArquivo));

        Id = Guid.NewGuid();
        NomeArquivo = nomeArquivo.Trim();
        Status = StatusImportacao.Pendente;
        CriadoEm = DateTime.UtcNow;
    }

    /// <summary>Chave do objeto no S3 — derivada do Id pra não depender do nome enviado pelo cliente.</summary>
    public string ChaveS3 => $"extratos/{Id}.csv";

    public bool JaFoiProcessada => Status is StatusImportacao.Concluida or StatusImportacao.Falhou;

    public void IniciarProcessamento()
    {
        if (Status != StatusImportacao.Pendente)
            throw new InvalidOperationException($"Importação em '{Status}' não pode iniciar processamento.");

        Status = StatusImportacao.Processando;
    }

    public void Concluir(int linhasImportadas, int linhasComErro)
    {
        if (Status != StatusImportacao.Processando)
            throw new InvalidOperationException($"Importação em '{Status}' não pode ser concluída.");
        if (linhasImportadas < 0 || linhasComErro < 0)
            throw new ArgumentException("Contadores não podem ser negativos.");

        Status = StatusImportacao.Concluida;
        LinhasImportadas = linhasImportadas;
        LinhasComErro = linhasComErro;
        ProcessadoEm = DateTime.UtcNow;
    }

    public void Falhar(string erro)
    {
        if (Status != StatusImportacao.Processando)
            throw new InvalidOperationException($"Importação em '{Status}' não pode ser marcada como falha.");

        Status = StatusImportacao.Falhou;
        Erro = erro;
        ProcessadoEm = DateTime.UtcNow;
    }
}

public enum StatusImportacao
{
    Pendente = 1,
    Processando = 2,
    Concluida = 3,
    Falhou = 4
}
