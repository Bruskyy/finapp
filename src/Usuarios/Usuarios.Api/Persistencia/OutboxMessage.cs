namespace Usuarios.Api.Persistencia;

/// <summary>
/// Outbox pattern (primeira vez em Usuarios.Api - já usado em Lancamentos e
/// Gamificacao): grava o evento na mesma transação/SaveChanges do dado de
/// domínio; um BackgroundService publica os pendentes no RabbitMQ depois.
/// Evita a inconsistência de publicar direto no broker dentro do handler
/// (banco e broker não compartilham transação).
/// </summary>
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
