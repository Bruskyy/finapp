using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Usuarios.Api.Contratos;
using Usuarios.Api.Dominio;

namespace Usuarios.Tests;

public class PerfilOnboardingEndpointsTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public PerfilOnboardingEndpointsTests(PostgresFixture fixture)
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

    private static PerfilOnboardingRequest RequestValido() => new(
        MomentoDeVida.PrimeiroEmprego,
        MaiorObjetivo.Notebook,
        null,
        500m,
        3000m,
        MaiorDificuldade.NaoConsigoGuardar);

    private async Task<HttpClient> RegistrarELogarAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var registro = await client.PostAsJsonAsync("/registrar", new RegistrarRequest("Vitor", $"{Guid.NewGuid()}@teste.com", "Senha123!"));
        var token = (await registro.Content.ReadFromJsonAsync<TokenResponse>())!.Token;
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return client;
    }

    [Fact]
    public async Task Registrar_UsuarioNovo_OnboardingComecaNaoConcluido()
    {
        await using var factory = CriarFactory();
        var client = await RegistrarELogarAsync(factory);

        var me = await client.GetFromJsonAsync<UsuarioResponse>("/me");

        Assert.False(me!.OnboardingConcluido);
    }

    [Fact]
    public async Task PerfilOnboarding_ComDadosValidos_DeveMarcarConcluido()
    {
        await using var factory = CriarFactory();
        var client = await RegistrarELogarAsync(factory);

        var resposta = await client.PutAsJsonAsync("/perfil-onboarding", RequestValido());

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<UsuarioResponse>();
        Assert.True(corpo!.OnboardingConcluido);

        var me = await client.GetFromJsonAsync<UsuarioResponse>("/me");
        Assert.True(me!.OnboardingConcluido);
    }

    [Fact]
    public async Task PerfilOnboarding_ComObjetivoOutroSemNome_DeveRetornar400()
    {
        await using var factory = CriarFactory();
        var client = await RegistrarELogarAsync(factory);

        var req = RequestValido() with { MaiorObjetivo = MaiorObjetivo.Outro, NomeObjetivoPersonalizado = null };
        var resposta = await client.PutAsJsonAsync("/perfil-onboarding", req);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task PerfilOnboarding_ComObjetivoOutroComNome_DeveAceitar()
    {
        await using var factory = CriarFactory();
        var client = await RegistrarELogarAsync(factory);

        var req = RequestValido() with { MaiorObjetivo = MaiorObjetivo.Outro, NomeObjetivoPersonalizado = "Curso de inglês" };
        var resposta = await client.PutAsJsonAsync("/perfil-onboarding", req);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task PerfilOnboarding_ComValorMensalZero_DeveRetornar400()
    {
        await using var factory = CriarFactory();
        var client = await RegistrarELogarAsync(factory);

        var req = RequestValido() with { ValorMensalDesejado = 0 };
        var resposta = await client.PutAsJsonAsync("/perfil-onboarding", req);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task PularOnboarding_DeveMarcarConcluidoSemExigirDados()
    {
        await using var factory = CriarFactory();
        var client = await RegistrarELogarAsync(factory);

        var resposta = await client.PostAsync("/perfil-onboarding/pular", null);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<UsuarioResponse>();
        Assert.True(corpo!.OnboardingConcluido);

        var me = await client.GetFromJsonAsync<UsuarioResponse>("/me");
        Assert.True(me!.OnboardingConcluido);
    }

    [Fact]
    public async Task PerfilOnboarding_SemToken_DeveRetornar401()
    {
        await using var factory = CriarFactory();
        using var client = factory.CreateClient();

        var resposta = await client.PutAsJsonAsync("/perfil-onboarding", RequestValido());

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }
}
