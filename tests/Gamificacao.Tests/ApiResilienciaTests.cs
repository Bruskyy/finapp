using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;

namespace Gamificacao.Tests;

/// <summary>
/// Regressão do bug "502 na aba Moedas": os consumers RabbitMQ conectavam uma
/// única vez no boot e, se o broker não estivesse pronto (ex: máquina recém
/// reiniciada), a exceção derrubava o host inteiro (StopHost) — o Gateway então
/// devolvia 502 porque não havia ninguém na porta. A API deve subir e continuar
/// servindo HTTP mesmo com o RabbitMQ fora do ar.
/// </summary>
public class ApiResilienciaTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public ApiResilienciaTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Api_SemRabbitMqDisponivel_DeveContinuarServindoHttp()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:GamificacaoDb", _fixture.ConnectionString);
                builder.UseSetting("RabbitMq:HostName", "localhost");
                builder.UseSetting("RabbitMq:Port", "1"); // porta fechada: conexão sempre recusada
                // chave fixa só para os testes - não depende do user-secrets local,
                // senão os testes quebrariam no CI (onde não existe secrets.json).
                builder.UseSetting("Jwt:SecretKey", TokenDeTeste.SecretKey);
                builder.UseSetting("Jwt:Issuer", "FinApp");
                builder.UseSetting("Jwt:Audience", "FinApp.Clientes");
            });

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TokenDeTeste.Gerar()}");

        // dá tempo dos BackgroundServices tentarem (e falharem) a primeira conexão
        await Task.Delay(TimeSpan.FromSeconds(2));

        var health = await client.GetAsync("/health");
        var saldo = await client.GetAsync("/saldo");

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal(HttpStatusCode.OK, saldo.StatusCode);
    }
}
