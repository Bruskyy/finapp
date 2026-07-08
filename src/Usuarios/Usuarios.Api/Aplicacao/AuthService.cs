using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Usuarios.Api.Contratos;
using Usuarios.Api.Dominio;
using Usuarios.Api.Persistencia;

namespace Usuarios.Api.Aplicacao;

public class EmailJaExisteException : Exception
{
    public EmailJaExisteException() : base("Já existe uma conta com este e-mail.") { }
}

public class CredenciaisInvalidasException : Exception
{
    public CredenciaisInvalidasException() : base("Email ou senha inválidos.") { }
}

public class SenhaAtualIncorretaException : Exception
{
    public SenhaAtualIncorretaException() : base("Senha atual incorreta.") { }
}

public class UsuarioNaoEncontradoException : Exception
{
    public UsuarioNaoEncontradoException() : base("Usuário não encontrado.") { }
}

public class TokenGoogleInvalidoException : Exception
{
    public TokenGoogleInvalidoException() : base("Token do Google inválido.") { }
}

public class AuthService
{
    private readonly IUsuarioRepository _repositorio;
    private readonly IPasswordHasher<Usuario> _hasher;
    private readonly RefreshTokenService _refreshTokens;
    private readonly IGoogleIdTokenValidator _googleValidator;
    private readonly string _googleClientId;

    public AuthService(
        IUsuarioRepository repositorio,
        IPasswordHasher<Usuario> hasher,
        RefreshTokenService refreshTokens,
        IGoogleIdTokenValidator googleValidator,
        IConfiguration configuracao)
    {
        _repositorio = repositorio;
        _hasher = hasher;
        _refreshTokens = refreshTokens;
        _googleValidator = googleValidator;
        _googleClientId = configuracao["Google:ClientId"]
            ?? throw new InvalidOperationException("Configuração 'Google:ClientId' ausente.");
    }

    public async Task<TokenResponse> RegistrarAsync(string nome, string email, string senha, CancellationToken ct)
    {
        var existente = await _repositorio.ObterPorEmailAsync(email, ct);
        if (existente is not null)
            throw new EmailJaExisteException();

        // HashPassword recebe um Usuario só por assinatura da interface (permite
        // implementações que incorporem dados do usuário no hash); a
        // implementação padrão do ASP.NET Core não usa esse parâmetro, então
        // é seguro (e é o padrão aceito) passar null aqui, antes de o usuário
        // existir de fato.
        var hash = _hasher.HashPassword(null!, senha);
        var usuario = new Usuario(nome, email, hash);

        var sucesso = await _repositorio.AdicionarAsync(usuario, ct);
        if (!sucesso)
            throw new EmailJaExisteException();

        var tokens = await _refreshTokens.GerarParAsync(usuario, ct);
        return new TokenResponse(tokens.AccessToken, tokens.RefreshToken, usuario.Nome, usuario.Email);
    }

    public async Task<TokenResponse> LoginAsync(string email, string senha, CancellationToken ct)
    {
        var usuario = await _repositorio.ObterPorEmailAsync(email, ct);
        // SenhaHash nulo = conta criada via Google, sem senha própria pra
        // verificar. Mesma mensagem genérica dos outros casos - não confirma
        // pra quem tenta adivinhar se o e-mail existe (user enumeration).
        if (usuario is null || usuario.SenhaHash is null)
            throw new CredenciaisInvalidasException();

        var resultado = _hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, senha);
        if (resultado == PasswordVerificationResult.Failed)
            throw new CredenciaisInvalidasException();

        var tokens = await _refreshTokens.GerarParAsync(usuario, ct);
        return new TokenResponse(tokens.AccessToken, tokens.RefreshToken, usuario.Nome, usuario.Email);
    }

    public async Task<TokenResponse> LoginComGoogleAsync(string idToken, CancellationToken ct)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            // Valida a assinatura do token contra as chaves públicas do Google
            // (JWKS) e confere que foi emitido especificamente pro nosso
            // Client ID (Audience) - nunca confiamos em dados decodificados
            // sem validar a assinatura primeiro.
            payload = await _googleValidator.ValidarAsync(idToken, _googleClientId);
        }
        catch (InvalidJwtException)
        {
            throw new TokenGoogleInvalidoException();
        }

        // "Encontrar ou criar" pelo e-mail: se já existe conta (com ou sem
        // senha) para este e-mail, o login Google autentica a mesma conta -
        // e-mail é a identidade, independente de como o usuário entrou.
        var usuario = await _repositorio.ObterPorEmailAsync(payload.Email, ct);
        if (usuario is null)
        {
            usuario = Usuario.CriarComGoogle(payload.Name, payload.Email);
            await _repositorio.AdicionarAsync(usuario, ct);
        }

        var tokens = await _refreshTokens.GerarParAsync(usuario, ct);
        return new TokenResponse(tokens.AccessToken, tokens.RefreshToken, usuario.Nome, usuario.Email);
    }

    public async Task<UsuarioResponse> AtualizarPerfilAsync(Guid usuarioId, string novoNome, CancellationToken ct)
    {
        var usuario = await _repositorio.ObterPorIdAsync(usuarioId, ct);
        if (usuario is null)
            throw new UsuarioNaoEncontradoException();

        usuario.AtualizarNome(novoNome);
        await _repositorio.AtualizarAsync(usuario, ct);

        return new UsuarioResponse(usuario.Id, usuario.Nome, usuario.Email, usuario.CriadoEm);
    }

    public async Task TrocarSenhaAsync(Guid usuarioId, string senhaAtual, string novaSenha, CancellationToken ct)
    {
        var usuario = await _repositorio.ObterPorIdAsync(usuarioId, ct);
        if (usuario is null)
            throw new UsuarioNaoEncontradoException();

        // Conta Google não tem senha própria - nada pra "trocar" ainda
        // (permitir definir uma senha do zero seria outra feature).
        if (usuario.SenhaHash is null ||
            _hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, senhaAtual) == PasswordVerificationResult.Failed)
            throw new SenhaAtualIncorretaException();

        var novoHash = _hasher.HashPassword(usuario, novaSenha);
        usuario.AtualizarSenhaHash(novoHash);
        await _repositorio.AtualizarAsync(usuario, ct);
    }
}
