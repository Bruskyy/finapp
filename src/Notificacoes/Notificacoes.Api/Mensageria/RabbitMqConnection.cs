using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Notificacoes.Api.Mensageria;

public class RabbitMqConnection : IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public RabbitMqConnection(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    public async Task<IChannel> CriarCanalAsync(CancellationToken ct = default)
    {
        var conexao = await ObterConexaoAsync(ct);
        var canal = await conexao.CreateChannelAsync(cancellationToken: ct);
        await canal.ExchangeDeclareAsync(_options.ExchangeGamificacao, ExchangeType.Topic, durable: true, cancellationToken: ct);
        return canal;
    }

    private async Task<IConnection> ObterConexaoAsync(CancellationToken ct)
    {
        if (_connection is { IsOpen: true })
            return _connection;

        await _lock.WaitAsync(ct);
        try
        {
            if (_connection is { IsOpen: true })
                return _connection;

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password
            };
            _connection = await factory.CreateConnectionAsync(ct);
            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}
