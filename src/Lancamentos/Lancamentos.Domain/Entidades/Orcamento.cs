namespace Lancamentos.Domain.Entidades;

/// <summary>
/// Teto de gastos mensal para uma categoria (estilo Mobills): o mesmo limite
/// vale para todos os meses até ser alterado ou removido.
/// </summary>
public class Orcamento
{
    public Guid Id { get; private set; }
    public Guid CategoriaId { get; private set; }
    public decimal ValorLimite { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public DateTime CriadoEm { get; private set; }

    private Orcamento() { }

    public Orcamento(Guid categoriaId, decimal valorLimite, Guid usuarioId)
    {
        if (valorLimite <= 0)
            throw new ArgumentException("Valor limite deve ser maior que zero.", nameof(valorLimite));
        if (categoriaId == Guid.Empty)
            throw new ArgumentException("Categoria é obrigatória.", nameof(categoriaId));

        Id = Guid.NewGuid();
        CategoriaId = categoriaId;
        ValorLimite = valorLimite;
        UsuarioId = usuarioId;
        CriadoEm = DateTime.UtcNow;
    }

    public void AlterarLimite(decimal valorLimite)
    {
        if (valorLimite <= 0)
            throw new ArgumentException("Valor limite deve ser maior que zero.", nameof(valorLimite));

        ValorLimite = valorLimite;
    }
}
