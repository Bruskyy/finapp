namespace Gamificacao.Api.Dominio;

public enum TipoMovimento
{
    Credito = 1,
    Debito = 2
}

public class MovimentoMoedas
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public int Quantidade { get; private set; }
    public TipoMovimento Tipo { get; private set; }
    public string Motivo { get; private set; }
    public DateTime CriadoEm { get; private set; }

    private MovimentoMoedas() { Motivo = null!; }

    public MovimentoMoedas(Guid eventId, int quantidade, TipoMovimento tipo, string motivo)
    {
        if (quantidade <= 0)
            throw new ArgumentException("Quantidade deve ser maior que zero.", nameof(quantidade));
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("Motivo é obrigatório.", nameof(motivo));

        Id = Guid.NewGuid();
        EventId = eventId;
        Quantidade = quantidade;
        Tipo = tipo;
        Motivo = motivo.Trim();
        CriadoEm = DateTime.UtcNow;
    }
}
