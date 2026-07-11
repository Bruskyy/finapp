namespace Lancamentos.Domain.Entidades;

public class Lancamento
{
    public Guid Id { get; private set; }
    public string Descricao { get; private set; }
    public decimal Valor { get; private set; }
    public TipoLancamento Tipo { get; private set; }
    public Guid CategoriaId { get; private set; }
    public Guid ContaId { get; private set; }
    public DateTime Data { get; private set; }
    public DateTime CriadoEm { get; private set; }

    /// <summary>
    /// Dono do lançamento. Nullable só pra tolerar registros criados antes da
    /// autenticação existir (ver README, "Zero trust real") - todo lançamento
    /// novo, a partir de agora, sempre tem um dono real.
    /// </summary>
    public Guid? UsuarioId { get; private set; }

    /// <summary>Preenchido quando o lançamento foi materializado por uma conta fixa (badge "recorrente" no app).</summary>
    public Guid? RecorrenciaId { get; private set; }

    /// <summary>
    /// Mês da fatura (sempre dia 1) - só existe quando a conta é cartão de
    /// crédito. A fatura é derivada da soma das competências, nunca
    /// materializada (ITEM-CARTAO-CREDITO.md, decisão 2).
    /// </summary>
    public DateTime? Competencia { get; private set; }

    /// <summary>Vínculo com a compra-mãe quando este lançamento é uma parcela.</summary>
    public Guid? CompraParceladaId { get; private set; }
    public int? NumeroParcela { get; private set; }

    private readonly List<Tag> _tags = new();

    /// <summary>Etiquetas livres (N:N via skip navigation do EF Core).</summary>
    public IReadOnlyCollection<Tag> Tags => _tags.AsReadOnly();

    private Lancamento() { Descricao = null!; }

    public Lancamento(string descricao, decimal valor, TipoLancamento tipo, Guid categoriaId, Guid contaId, DateTime data, Guid usuarioId)
    {
        Validar(descricao, valor, contaId);

        Id = Guid.NewGuid();
        Descricao = descricao.Trim();
        Valor = valor;
        Tipo = tipo;
        CategoriaId = categoriaId;
        ContaId = contaId;
        Data = data;
        UsuarioId = usuarioId;
        CriadoEm = DateTime.UtcNow;
    }

    /// <summary>Factory para lançamentos materializados por uma conta fixa - o dono vem da
    /// própria recorrência (nullable pra recorrências legadas sem dono).</summary>
    public static Lancamento CriarDeRecorrencia(
        string descricao, decimal valor, TipoLancamento tipo, Guid categoriaId, Guid contaId, DateTime data, Guid recorrenciaId, Guid? usuarioId)
    {
        Validar(descricao, valor, contaId);

        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            Descricao = descricao.Trim(),
            Valor = valor,
            Tipo = tipo,
            CategoriaId = categoriaId,
            ContaId = contaId,
            Data = data,
            UsuarioId = usuarioId,
            CriadoEm = DateTime.UtcNow,
            RecorrenciaId = recorrenciaId,
        };
        return lancamento;
    }

    public void Atualizar(string descricao, decimal valor, TipoLancamento tipo, Guid categoriaId, Guid contaId, DateTime data)
    {
        Validar(descricao, valor, contaId);

        Descricao = descricao.Trim();
        Valor = valor;
        Tipo = tipo;
        CategoriaId = categoriaId;
        ContaId = contaId;
        Data = data;
    }

    /// <summary>
    /// Factory pra parcela de compra parcelada (sempre despesa) - chamada só
    /// por <see cref="CompraParcelada.GerarParcelas"/>, que é quem conhece a
    /// regra de divisão/competência.
    /// </summary>
    public static Lancamento CriarParcela(
        string descricao, decimal valor, Guid categoriaId, Guid contaId, DateTime data,
        DateTime competencia, Guid compraParceladaId, int numeroParcela, Guid? usuarioId)
    {
        Validar(descricao, valor, contaId);

        return new Lancamento
        {
            Id = Guid.NewGuid(),
            Descricao = descricao.Trim(),
            Valor = valor,
            Tipo = TipoLancamento.Despesa,
            CategoriaId = categoriaId,
            ContaId = contaId,
            Data = data,
            UsuarioId = usuarioId,
            CriadoEm = DateTime.UtcNow,
            Competencia = competencia,
            CompraParceladaId = compraParceladaId,
            NumeroParcela = numeroParcela,
        };
    }

    /// <summary>
    /// Recalcula a competência a partir da conta (chamar na criação e sempre
    /// que Data/Conta mudarem): vira null quando a conta não é cartão.
    /// </summary>
    public void AtribuirCompetencia(Conta conta)
    {
        if (conta.Id != ContaId)
            throw new InvalidOperationException("Conta informada não é a conta do lançamento.");
        Competencia = conta.CompetenciaPara(Data);
    }

    /// <summary>Substitui o conjunto de tags do lançamento.</summary>
    public void DefinirTags(IEnumerable<Tag> tags)
    {
        _tags.Clear();
        foreach (var tag in tags.DistinctBy(t => t.Nome))
            _tags.Add(tag);
    }

    private static void Validar(string descricao, decimal valor, Guid contaId)
    {
        if (valor <= 0)
            throw new ArgumentException("Valor deve ser maior que zero.", nameof(valor));
        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("Descrição é obrigatória.", nameof(descricao));
        if (contaId == Guid.Empty)
            throw new ArgumentException("Conta é obrigatória.", nameof(contaId));
    }
}

public enum TipoLancamento
{
    Receita = 1,
    Despesa = 2
}
