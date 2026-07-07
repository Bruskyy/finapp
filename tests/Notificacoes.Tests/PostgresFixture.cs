using Microsoft.EntityFrameworkCore;
using Notificacoes.Api.Persistencia;
using Testcontainers.PostgreSql;

namespace Notificacoes.Tests;

public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<NotificacoesDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using var db = new NotificacoesDbContext(options);
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
