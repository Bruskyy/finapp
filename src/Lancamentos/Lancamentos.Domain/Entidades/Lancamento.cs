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

    /// <summary>Preenchido quando o lançamento foi materializado por uma conta fixa (badge "recorrente" no app).</summary>
    public Guid? RecorrenciaId { get; private set; }

    private readonly List<Tag> _tags = new();

    /// <summary>Etiquetas livres (N:N via skip navigation do EF Core).</summary>
    public IReadOnlyCollection<Tag> Tags => _tags.AsReadOnly();

    private Lancamento() { Descricao = null!; }

    public Lancamento(string descricao, decimal valor, TipoLancamento tipo, Guid categoriaId, Guid contaId, DateTime data)
    {
        Validar(descricao, valor, contaId);

        Id = Guid.NewGuid();
        Descricao = descricao.Trim();
        Valor = valor;
        Tipo = tipo;
        CategoriaId = categoriaId;
        ContaId = contaId;
        Data = data;
        CriadoEm = DateTime.UtcNow;
    }

    /// <summary>Factory para lançamentos materializados por uma conta fixa.</summary>
    public static Lancamento CriarDeRecorrencia(
        string descricao, decimal valor, TipoLancamento tipo, Guid categoriaId, Guid contaId, DateTime data, Guid recorrenciaId)
    {
        var lancamento = new Lancamento(descricao, valor, tipo, categoriaId, contaId, data);
        lancamento.RecorrenciaId = recorrenciaId;
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
