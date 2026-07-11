namespace Lancamentos.Domain.Entidades;

/// <summary>
/// Caixa de dinheiro (estilo Mobills): Carteira, Banco X, poupança etc.
/// Todo lançamento pertence a exatamente uma conta. Cartão de crédito é uma
/// conta com <see cref="TipoConta.Cartao"/> (ver ITEM-CARTAO-CREDITO.md) -
/// os campos de cartão só existem nesse tipo, garantido pelas factories.
/// </summary>
public class Conta
{
    /// <summary>
    /// Conta padrão criada pela migration de backfill — lançamentos anteriores
    /// à existência de contas foram atribuídos a ela, e é o fallback da
    /// importação de CSV (o extrato não tem coluna de conta).
    /// </summary>
    public static readonly Guid CarteiraPadraoId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public TipoConta Tipo { get; private set; }

    // Campos exclusivos de cartão de crédito - null em conta corrente
    // (invariante garantida pelas factories).
    public decimal? Limite { get; private set; }
    public int? DiaFechamento { get; private set; }
    public int? DiaVencimento { get; private set; }

    public bool EhCartao => Tipo == TipoConta.Cartao;

    private Conta() { Nome = null!; }

    public Conta(string nome, Guid usuarioId)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome é obrigatório.", nameof(nome));

        Id = Guid.NewGuid();
        Nome = nome.Trim();
        UsuarioId = usuarioId;
        Tipo = TipoConta.Corrente;
        CriadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Cartão de crédito. Dias limitados a 1-28 de propósito: evita a classe
    /// inteira de bugs de dia 29/30/31 + fevereiro (bancos reais fazem o
    /// mesmo) - simplificação deliberada documentada no README.
    /// </summary>
    public static Conta CriarCartao(string nome, decimal limite, int diaFechamento, int diaVencimento, Guid usuarioId)
    {
        if (limite <= 0)
            throw new ArgumentException("Limite deve ser maior que zero.", nameof(limite));
        if (diaFechamento is < 1 or > 28)
            throw new ArgumentException("Dia de fechamento deve estar entre 1 e 28.", nameof(diaFechamento));
        if (diaVencimento is < 1 or > 28)
            throw new ArgumentException("Dia de vencimento deve estar entre 1 e 28.", nameof(diaVencimento));
        if (diaFechamento == diaVencimento)
            throw new ArgumentException("Fechamento e vencimento não podem ser no mesmo dia.", nameof(diaVencimento));

        return new Conta(nome, usuarioId)
        {
            Tipo = TipoConta.Cartao,
            Limite = limite,
            DiaFechamento = diaFechamento,
            DiaVencimento = diaVencimento,
        };
    }

    /// <summary>
    /// Competência (mês da fatura, sempre dia 1) de uma compra nesta conta:
    /// até o dia do fechamento entra na fatura do mês corrente; depois dele,
    /// na do mês seguinte. Null pra conta corrente - competência é conceito
    /// exclusivo de cartão (a fatura é DERIVADA da soma das competências,
    /// nunca materializada - ver ITEM-CARTAO-CREDITO.md, decisão 2).
    /// </summary>
    public DateTime? CompetenciaPara(DateTime data)
    {
        if (!EhCartao) return null;

        var competencia = new DateTime(data.Year, data.Month, 1);
        return data.Day <= DiaFechamento!.Value ? competencia : competencia.AddMonths(1);
    }
}
