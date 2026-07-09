using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notificacoes.Api.Contratos;
using Notificacoes.Api.Persistencia;

namespace Notificacoes.Tests;

public class DispositivosEndpointsTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public DispositivosEndpointsTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private WebApplicationFactory<Program> CriarFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:NotificacoesDb", _fixture.ConnectionString);
            builder.UseSetting("RabbitMq:HostName", "localhost");
            builder.UseSetting("RabbitMq:Port", "1"); // porta fechada: conexão sempre recusada
            builder.UseSetting("Jwt:SecretKey", TokenDeTeste.SecretKey);
            builder.UseSetting("Jwt:Issuer", "FinApp");
            builder.UseSetting("Jwt:Audience", "FinApp.Clientes");
        });

    [Fact]
    public async Task PostDispositivos_SemToken_DeveDevolver401()
    {
        await using var factory = CriarFactory();
        using var client = factory.CreateClient();

        var resposta = await client.PostAsJsonAsync("/dispositivos", new RegistrarDispositivoRequest("expo-token-x"));

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task PostDispositivos_ComTokenValido_Registra204EPersisteNoBanco()
    {
        await using var factory = CriarFactory();
        var usuarioId = Guid.NewGuid();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TokenDeTeste.Gerar(usuarioId)}");

        var resposta = await client.PostAsJsonAsync("/dispositivos", new RegistrarDispositivoRequest("expo-token-valido"));

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);

        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IDispositivoPushRepository>();
        var tokens = await repo.ListarTokensAsync(usuarioId, CancellationToken.None);
        Assert.Contains("expo-token-valido", tokens);
    }

    [Fact]
    public async Task PostDispositivos_ComTokenVazio_Devolve400()
    {
        await using var factory = CriarFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TokenDeTeste.Gerar()}");

        var resposta = await client.PostAsJsonAsync("/dispositivos", new RegistrarDispositivoRequest(""));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task DeleteDispositivos_TokenRegistrado_RemoveEDevolve204()
    {
        await using var factory = CriarFactory();
        var usuarioId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IDispositivoPushRepository>();
            await repo.RegistrarAsync(usuarioId, "expo-token-a-remover", CancellationToken.None);
        }

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TokenDeTeste.Gerar(usuarioId)}");
        var requisicao = new HttpRequestMessage(HttpMethod.Delete, "/dispositivos")
        {
            Content = JsonContent.Create(new RegistrarDispositivoRequest("expo-token-a-remover")),
        };

        var resposta = await client.SendAsync(requisicao);

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        using var scopeVerificacao = factory.Services.CreateScope();
        var repoVerificacao = scopeVerificacao.ServiceProvider.GetRequiredService<IDispositivoPushRepository>();
        var tokens = await repoVerificacao.ListarTokensAsync(usuarioId, CancellationToken.None);
        Assert.DoesNotContain("expo-token-a-remover", tokens);
    }
}
