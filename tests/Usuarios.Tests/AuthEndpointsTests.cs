using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Usuarios.Api.Contratos;

namespace Usuarios.Tests;

public class AuthEndpointsTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public AuthEndpointsTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private WebApplicationFactory<Program> CriarFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:UsuariosDb", _fixture.ConnectionString);
            // chave fixa só para os testes - não depende do user-secrets local,
            // senão os testes quebrariam no CI (onde não existe secrets.json).
            builder.UseSetting("Jwt:SecretKey", "chave-de-teste-para-ci-com-pelo-menos-32-bytes-0000");
            builder.UseSetting("Jwt:Issuer", "FinApp");
            builder.UseSetting("Jwt:Audience", "FinApp.Clientes");
            builder.UseSetting("Jwt:ExpiracaoMinutos", "60");
        });

    [Fact]
    public async Task Registrar_ComDadosValidos_DeveRetornar201ComToken()
    {
        await using var factory = CriarFactory();
        using var client = factory.CreateClient();
        var email = $"{Guid.NewGuid()}@teste.com";

        var resposta = await client.PostAsJsonAsync("/registrar", new RegistrarRequest("Vitor", email, "Senha123!"));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(corpo);
        Assert.False(string.IsNullOrWhiteSpace(corpo!.Token));
        Assert.Equal(email, corpo.Email);
    }

    [Fact]
    public async Task Registrar_ComEmailDuplicado_DeveRetornar409()
    {
        await using var factory = CriarFactory();
        using var client = factory.CreateClient();
        var email = $"{Guid.NewGuid()}@teste.com";

        await client.PostAsJsonAsync("/registrar", new RegistrarRequest("Vitor", email, "Senha123!"));
        var resposta = await client.PostAsJsonAsync("/registrar", new RegistrarRequest("Outro", email, "OutraSenha123!"));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Login_ComSenhaCorreta_DeveRetornar200ComToken()
    {
        await using var factory = CriarFactory();
        using var client = factory.CreateClient();
        var email = $"{Guid.NewGuid()}@teste.com";
        await client.PostAsJsonAsync("/registrar", new RegistrarRequest("Vitor", email, "Senha123!"));

        var resposta = await client.PostAsJsonAsync("/login", new LoginRequest(email, "Senha123!"));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.False(string.IsNullOrWhiteSpace(corpo!.Token));
    }

    [Fact]
    public async Task Login_ComSenhaErrada_DeveRetornar401()
    {
        await using var factory = CriarFactory();
        using var client = factory.CreateClient();
        var email = $"{Guid.NewGuid()}@teste.com";
        await client.PostAsJsonAsync("/registrar", new RegistrarRequest("Vitor", email, "Senha123!"));

        var resposta = await client.PostAsJsonAsync("/login", new LoginRequest(email, "senhaerrada"));

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Login_ComEmailInexistente_DeveRetornar401()
    {
        await using var factory = CriarFactory();
        using var client = factory.CreateClient();

        var resposta = await client.PostAsJsonAsync("/login", new LoginRequest("nao-existe@teste.com", "qualquer123"));

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Me_SemToken_DeveRetornar401()
    {
        await using var factory = CriarFactory();
        using var client = factory.CreateClient();

        var resposta = await client.GetAsync("/me");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Me_ComTokenValido_DeveRetornarDadosDoUsuario()
    {
        await using var factory = CriarFactory();
        using var client = factory.CreateClient();
        var email = $"{Guid.NewGuid()}@teste.com";
        var registro = await client.PostAsJsonAsync("/registrar", new RegistrarRequest("Vitor", email, "Senha123!"));
        var token = (await registro.Content.ReadFromJsonAsync<TokenResponse>())!.Token;

        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        var resposta = await client.GetAsync("/me");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var usuario = await resposta.Content.ReadFromJsonAsync<UsuarioResponse>();
        Assert.Equal(email, usuario!.Email);
        Assert.Equal("Vitor", usuario.Nome);
    }

    [Fact]
    public async Task Registrar_ComSenhaCurta_DeveRetornar400()
    {
        await using var factory = CriarFactory();
        using var client = factory.CreateClient();

        var resposta = await client.PostAsJsonAsync("/registrar", new RegistrarRequest("Vitor", $"{Guid.NewGuid()}@teste.com", "123"));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    private async Task<(HttpClient client, string token)> RegistrarELogarAsync(WebApplicationFactory<Program> factory, string email)
    {
        var client = factory.CreateClient();
        var registro = await client.PostAsJsonAsync("/registrar", new RegistrarRequest("Vitor", email, "Senha123!"));
        var token = (await registro.Content.ReadFromJsonAsync<TokenResponse>())!.Token;
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return (client, token);
    }

    [Fact]
    public async Task AtualizarPerfil_SemToken_DeveRetornar401()
    {
        await using var factory = CriarFactory();
        using var client = factory.CreateClient();

        var resposta = await client.PutAsJsonAsync("/perfil", new AtualizarPerfilRequest("Novo Nome"));

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task AtualizarPerfil_ComTokenValido_DeveAtualizarNome()
    {
        await using var factory = CriarFactory();
        var (client, _) = await RegistrarELogarAsync(factory, $"{Guid.NewGuid()}@teste.com");

        var resposta = await client.PutAsJsonAsync("/perfil", new AtualizarPerfilRequest("Novo Nome"));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var usuario = await resposta.Content.ReadFromJsonAsync<UsuarioResponse>();
        Assert.Equal("Novo Nome", usuario!.Nome);

        var me = await client.GetFromJsonAsync<UsuarioResponse>("/me");
        Assert.Equal("Novo Nome", me!.Nome);
    }

    [Fact]
    public async Task TrocarSenha_ComSenhaAtualErrada_DeveRetornar400()
    {
        await using var factory = CriarFactory();
        var (client, _) = await RegistrarELogarAsync(factory, $"{Guid.NewGuid()}@teste.com");

        var resposta = await client.PutAsJsonAsync("/senha", new TrocarSenhaRequest("senhaerrada", "NovaSenha123!"));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task TrocarSenha_ComSenhaAtualCorreta_DevePermitirLoginComNovaSenha()
    {
        await using var factory = CriarFactory();
        var email = $"{Guid.NewGuid()}@teste.com";
        var (client, _) = await RegistrarELogarAsync(factory, email);

        var resposta = await client.PutAsJsonAsync("/senha", new TrocarSenhaRequest("Senha123!", "NovaSenha123!"));
        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);

        using var clientSemAuth = factory.CreateClient();
        var loginAntiga = await clientSemAuth.PostAsJsonAsync("/login", new LoginRequest(email, "Senha123!"));
        Assert.Equal(HttpStatusCode.Unauthorized, loginAntiga.StatusCode);

        var loginNova = await clientSemAuth.PostAsJsonAsync("/login", new LoginRequest(email, "NovaSenha123!"));
        Assert.Equal(HttpStatusCode.OK, loginNova.StatusCode);
    }
}
