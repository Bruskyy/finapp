namespace Lancamentos.Infrastructure.Outbox;

public class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Tipo { get; private set; }
    public string Payload { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime? ProcessadoEm { get; private set; }

    private OutboxMessage() { Tipo = null!; Payload = null!; }

    public OutboxMessage(string tipo, string payload)
    {
        Id = Guid.NewGuid();
        Tipo = tipo;
        Payload = payload;
        CriadoEm = DateTime.UtcNow;
    }

    public void MarcarComoProcessada() => ProcessadoEm = DateTime.UtcNow;
}
