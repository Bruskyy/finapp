using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notificacoes.Api.Contratos;

namespace Notificacoes.Tests;

/// <summary>
/// Cobre o mesmo cenário de resiliência já validado em Gamificacao.Tests
/// (API tem que continuar servindo HTTP mesmo com o RabbitMQ fora do ar) e o
/// isolamento por usuário dos endpoints novos desta fase.
/// </summary>
public class NotificacoesEndpointsTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public NotificacoesEndpointsTests(PostgresFixture fixture)
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
    public async Task Api_SemRabbitMqDisponivel_DeveContinuarServindoHttp()
    {
        await using var factory = CriarFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TokenDeTeste.Gerar()}");

        await Task.Delay(TimeSpan.FromSeconds(2));

        var health = await client.GetAsync("/health");
        var notificacoes = await client.GetAsync("/notificacoes");

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal(HttpStatusCode.OK, notificacoes.StatusCode);
    }

    [Fact]
    public async Task GetNotificacoes_SemToken_DeveDevolver401()
    {
        await using var factory = CriarFactory();
        using var client = factory.CreateClient();

        var resposta = await client.GetAsync("/notificacoes");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task MarcarLida_NotificacaoInexistente_DeveDevolver404()
    {
        await using var factory = CriarFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TokenDeTeste.Gerar()}");

        var resposta = await client.PostAsync($"/notificacoes/{Guid.NewGuid()}/marcar-lida", null);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task GetNotificacoes_SoDevolveNotificacoesDoUsuarioDoToken()
    {
        await using var factory = CriarFactory();
        var usuarioId = Guid.NewGuid();

        // popula direto no banco (via factory.Services), simulando o que um
        // consumer RabbitMQ faria ao processar um evento
        using (var scope = factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<Notificacoes.Api.Persistencia.INotificacaoRepository>();
            await repo.AdicionarAsync(new Notificacoes.Api.Dominio.Notificacao(
                Guid.NewGuid(), Notificacoes.Api.Dominio.TipoNotificacao.Lancamento, "Minha notificação.", usuarioId), CancellationToken.None);
            await repo.AdicionarAsync(new Notificacoes.Api.Dominio.Notificacao(
                Guid.NewGuid(), Notificacoes.Api.Dominio.TipoNotificacao.Lancamento, "De outro usuário.", Guid.NewGuid()), CancellationToken.None);
        }

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {TokenDeTeste.Gerar(usuarioId)}");

        var resposta = await client.GetFromJsonAsync<List<NotificacaoResponse>>("/notificacoes");

        Assert.NotNull(resposta);
        Assert.Single(resposta);
        Assert.Equal("Minha notificação.", resposta[0].Mensagem);
    }
}
