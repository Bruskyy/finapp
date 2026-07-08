using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Usuarios.Api.Contratos;

namespace Usuarios.Tests;

public class RefreshTokenEndpointsTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public RefreshTokenEndpointsTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private WebApplicationFactory<Program> CriarFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:UsuariosDb", _fixture.ConnectionString);
            builder.UseSetting("Jwt:SecretKey", "chave-de-teste-para-ci-com-pelo-menos-32-bytes-0000");
            builder.UseSetting("Jwt:Issuer", "FinApp");
            builder.UseSetting("Jwt:Audience", "FinApp.Clientes");
            builder.UseSetting("Jwt:ExpiracaoMinutos", "60");
            builder.UseSetting("Jwt:RefreshExpiracaoDias", "30");
            builder.UseSetting("Google:ClientId", "teste.apps.googleusercontent.com");
        });

    private async Task<(HttpClient client, TokenResponse tokens)> RegistrarAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var email = $"{Guid.NewGuid()}@teste.com";
        var resposta = await client.PostAsJsonAsync("/registrar", new RegistrarRequest("Vitor", email, "Senha123!"));
        var tokens = (await resposta.Content.ReadFromJsonAsync<TokenResponse>())!;
        return (client, tokens);
    }

    [Fact]
    public async Task Refresh_ComTokenValido_DeveRetornarNovoParEFuncionarEmMe()
    {
        await using var factory = CriarFactory();
        var (client, tokens) = await RegistrarAsync(factory);

        var resposta = await client.PostAsJsonAsync("/refresh", new RenovarTokenRequest(tokens.RefreshToken));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var novoPar = await resposta.Content.ReadFromJsonAsync<RenovarTokenResponse>();
        Assert.False(string.IsNullOrWhiteSpace(novoPar!.Token));
        Assert.False(string.IsNullOrWhiteSpace(novoPar.RefreshToken));
        Assert.NotEqual(tokens.Token, novoPar.Token);
        Assert.NotEqual(tokens.RefreshToken, novoPar.RefreshToken);

        using var clientComNovoToken = factory.CreateClient();
        clientComNovoToken.DefaultRequestHeaders.Add("Authorization", $"Bearer {novoPar.Token}");
        var me = await clientComNovoToken.GetAsync("/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task Refresh_ComTokenReutilizadoAposRotacao_DeveRevogarFamiliaInteira()
    {
        await using var factory = CriarFactory();
        var (client, tokens) = await RegistrarAsync(factory);

        var primeiraRenovacao = await client.PostAsJsonAsync("/refresh", new RenovarTokenRequest(tokens.RefreshToken));
        var primeiroNovoPar = await primeiraRenovacao.Content.ReadFromJsonAsync<RenovarTokenResponse>();

        // reusa o refresh token ORIGINAL, que já foi rotacionado - sinal de roubo.
        var reuso = await client.PostAsJsonAsync("/refresh", new RenovarTokenRequest(tokens.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, reuso.StatusCode);

        // a família inteira foi revogada: nem o refresh token que a renovação
        // legítima (primeiraRenovacao) tinha acabado de receber funciona mais.
        var tentativaComTokenLegitimo = await client.PostAsJsonAsync(
            "/refresh", new RenovarTokenRequest(primeiroNovoPar!.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, tentativaComTokenLegitimo.StatusCode);
    }

    [Fact]
    public async Task Refresh_ComTokenInexistente_DeveRetornar401()
    {
        await using var factory = CriarFactory();
        using var client = factory.CreateClient();

        var resposta = await client.PostAsJsonAsync("/refresh", new RenovarTokenRequest("token-que-nao-existe"));

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Logout_RevogaToken_TentativaDeRefreshDepoisDeveFalhar()
    {
        await using var factory = CriarFactory();
        var (client, tokens) = await RegistrarAsync(factory);

        var logout = await client.PostAsJsonAsync("/logout", new RenovarTokenRequest(tokens.RefreshToken));
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var tentativaDeRefresh = await client.PostAsJsonAsync("/refresh", new RenovarTokenRequest(tokens.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, tentativaDeRefresh.StatusCode);
    }

    [Fact]
    public async Task Logout_ComTokenInexistente_DeveRetornar204Idempotente()
    {
        await using var factory = CriarFactory();
        using var client = factory.CreateClient();

        var resposta = await client.PostAsJsonAsync("/logout", new RenovarTokenRequest("token-que-nao-existe"));

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
    }
}
