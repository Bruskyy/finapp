using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Usuarios.Api.Persistencia;

namespace Usuarios.Tests;

public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<UsuariosDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using var db = new UsuariosDbContext(options);
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
