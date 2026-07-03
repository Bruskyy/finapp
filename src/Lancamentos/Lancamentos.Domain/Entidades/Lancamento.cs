namespace Lancamentos.Domain.Entidades;

public class Lancamento
{
    public Guid Id { get; private set; }
    public string Descricao { get; private set; }
    public decimal Valor { get; private set; }
    public TipoLancamento Tipo { get; private set; }
    public Guid CategoriaId { get; private set; }
    public DateTime Data { get; private set; }
    public DateTime CriadoEm { get; private set; }

    private Lancamento() { Descricao = null!; } 

    public Lancamento(string descricao, decimal valor, TipoLancamento tipo, Guid categoriaId, DateTime data)
    {
        if (valor <= 0)
            throw new ArgumentException("Valor deve ser maior que zero.", nameof(valor));
        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("Descrição é obrigatória.", nameof(descricao));

        Id = Guid.NewGuid();
        Descricao = descricao.Trim();
        Valor = valor;
        Tipo = tipo;
        CategoriaId = categoriaId;
        Data = data;
        CriadoEm = DateTime.UtcNow;
    }

    public void Atualizar(string descricao, decimal valor, TipoLancamento tipo, Guid categoriaId, DateTime data)
    {
        if (valor <= 0)
            throw new ArgumentException("Valor deve ser maior que zero.", nameof(valor));
        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("Descrição é obrigatória.", nameof(descricao));

        Descricao = descricao.Trim();
        Valor = valor;
        Tipo = tipo;
        CategoriaId = categoriaId;
        Data = data;
    }
}

public enum TipoLancamento
{
    Receita = 1,
    Despesa = 2
}