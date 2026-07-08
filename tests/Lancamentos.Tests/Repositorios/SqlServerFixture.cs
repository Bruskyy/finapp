using Lancamentos.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace Lancamentos.Tests.Repositorios;

public class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<LancamentosDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        await using var db = new LancamentosDbContext(options);
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
