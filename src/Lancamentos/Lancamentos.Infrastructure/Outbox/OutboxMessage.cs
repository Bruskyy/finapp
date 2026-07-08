namespace Lancamentos.Infrastructure.Outbox;

public class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Tipo { get; private set; }
    public string Payload { get; private set; }
    public CanalOutbox Canal { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime? ProcessadoEm { get; private set; }

    private OutboxMessage() { Tipo = null!; Payload = null!; }

    // Default RabbitMq preserva o comportamento de todo chamador existente
    // (LancamentoRepository etc.) sem precisar tocar nesses call sites.
    public OutboxMessage(string tipo, string payload, CanalOutbox canal = CanalOutbox.RabbitMq)
    {
        Id = Guid.NewGuid();
        Tipo = tipo;
        Payload = payload;
        Canal = canal;
        CriadoEm = DateTime.UtcNow;
    }

    public void MarcarComoProcessada() => ProcessadoEm = DateTime.UtcNow;
}
