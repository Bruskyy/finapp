namespace Lancamentos.Infrastructure.Outbox;

/// <summary>
/// Pra qual publicador uma linha da outbox pertence. A tabela OutboxMessages
/// é compartilhada entre canais diferentes (RabbitMQ, SQS) - cada
/// BackgroundService publicador só enxerga as próprias linhas via filtro
/// nesta coluna, senão o publicador do RabbitMQ tentaria (mal) publicar
/// mensagens destinadas ao SQS, e vice-versa.
/// </summary>
public enum CanalOutbox
{
    RabbitMq = 0,
    Sqs = 1,
}
