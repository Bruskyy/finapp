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

public class AuthService
{
    private readonly IUsuarioRepository _repositorio;
    private readonly IPasswordHasher<Usuario> _hasher;
    private readonly JwtTokenGenerator _jwtGenerator;

    public AuthService(IUsuarioRepository repositorio, IPasswordHasher<Usuario> hasher, JwtTokenGenerator jwtGenerator)
    {
        _repositorio = repositorio;
        _hasher = hasher;
        _jwtGenerator = jwtGenerator;
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

        return new TokenResponse(_jwtGenerator.GerarToken(usuario), usuario.Nome, usuario.Email);
    }

    public async Task<TokenResponse> LoginAsync(string email, string senha, CancellationToken ct)
    {
        var usuario = await _repositorio.ObterPorEmailAsync(email, ct);
        if (usuario is null)
            throw new CredenciaisInvalidasException();

        var resultado = _hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, senha);
        if (resultado == PasswordVerificationResult.Failed)
            throw new CredenciaisInvalidasException();

        return new TokenResponse(_jwtGenerator.GerarToken(usuario), usuario.Nome, usuario.Email);
    }
}
