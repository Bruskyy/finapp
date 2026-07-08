namespace Notificacoes.Api.Mensageria;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    // "/" é o vhost padrão do RabbitMQ local; provedores gerenciados (ex:
    // CloudAMQP) usam um vhost próprio por instância, normalmente igual ao
    // username.
    public string VirtualHost { get; set; } = "/";
    // Broker local (Docker Compose) não usa TLS; provedores gerenciados exigem.
    public bool UsarTls { get; set; } = false;
    public string ExchangeGamificacao { get; set; } = "finapp.gamificacao";
    public string ExchangeLancamentos { get; set; } = "finapp.lancamentos";
}
