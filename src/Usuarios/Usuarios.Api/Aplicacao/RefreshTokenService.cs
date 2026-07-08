using System.Security.Cryptography;
using Usuarios.Api.Dominio;
using Usuarios.Api.Persistencia;

namespace Usuarios.Api.Aplicacao;

public class RefreshTokenInvalidoException : Exception
{
    public RefreshTokenInvalidoException() : base("Refresh token inválido ou expirado.") { }
}

/// <summary>
/// Refresh token já foi usado antes (reuso de um token já rotacionado) —
/// sinal de possível roubo. Todos os tokens do usuário são revogados: o
/// cliente deve forçar logout completo, não só tentar de novo.
/// </summary>
public class RefreshTokenReutilizadoException : Exception
{
    public RefreshTokenReutilizadoException() : base("Sessão inválida — faça login novamente.") { }
}

public record ParDeTokens(string AccessToken, string RefreshToken);

public class RefreshTokenService
{
    private readonly IRefreshTokenRepository _repositorio;
    private readonly IUsuarioRepository _usuarios;
    private readonly JwtTokenGenerator _jwtGenerator;
    private readonly int _refreshExpiracaoDias;

    public RefreshTokenService(
        IRefreshTokenRepository repositorio,
        IUsuarioRepository usuarios,
        JwtTokenGenerator jwtGenerator,
        IConfiguration configuracao)
    {
        _repositorio = repositorio;
        _usuarios = usuarios;
        _jwtGenerator = jwtGenerator;
        _refreshExpiracaoDias = configuracao.GetValue<int>("Jwt:RefreshExpiracaoDias");
    }

    public async Task<ParDeTokens> GerarParAsync(Usuario usuario, CancellationToken ct)
    {
        var (tokenBruto, hash) = GerarTokenOpaco();
        var refreshToken = new RefreshToken(usuario.Id, hash, DateTime.UtcNow.AddDays(_refreshExpiracaoDias));
        await _repositorio.AdicionarAsync(refreshToken, ct);

        return new ParDeTokens(_jwtGenerator.GerarToken(usuario), tokenBruto);
    }

    public async Task<ParDeTokens> RenovarAsync(string refreshTokenBruto, CancellationToken ct)
    {
        var hash = HashDoToken(refreshTokenBruto);
        var tokenExistente = await _repositorio.ObterPorHashAsync(hash, ct);

        if (tokenExistente is null || tokenExistente.ExpiraEm <= DateTime.UtcNow)
            throw new RefreshTokenInvalidoException();

        if (tokenExistente.RevogadoEm is not null)
        {
            // Reuso de um token já trocado numa rotação anterior: alguém
            // (o dono legítimo num outro fluxo, ou um atacante com uma
            // cópia antiga) está usando uma credencial que já deveria
            // estar morta. Não dá pra distinguir os dois casos, então o
            // mais seguro é tratar como comprometimento e matar a sessão
            // inteira do usuário.
            await _repositorio.RevogarTodosDoUsuarioAsync(tokenExistente.UsuarioId, ct);
            throw new RefreshTokenReutilizadoException();
        }

        var usuario = await _usuarios.ObterPorIdAsync(tokenExistente.UsuarioId, ct);
        if (usuario is null)
            throw new RefreshTokenInvalidoException();

        var (tokenBruto, novoHash) = GerarTokenOpaco();
        var novoRefreshToken = new RefreshToken(usuario.Id, novoHash, DateTime.UtcNow.AddDays(_refreshExpiracaoDias));
        await _repositorio.AdicionarAsync(novoRefreshToken, ct);

        tokenExistente.Revogar(novoRefreshToken.Id);
        await _repositorio.AtualizarAsync(tokenExistente, ct);

        return new ParDeTokens(_jwtGenerator.GerarToken(usuario), tokenBruto);
    }

    public async Task RevogarAsync(string refreshTokenBruto, CancellationToken ct)
    {
        var hash = HashDoToken(refreshTokenBruto);
        var token = await _repositorio.ObterPorHashAsync(hash, ct);
        if (token is null || token.RevogadoEm is not null)
            return; // idempotente - não vaza se o token existia ou não

        token.Revogar();
        await _repositorio.AtualizarAsync(token, ct);
    }

    private static (string TokenBruto, string Hash) GerarTokenOpaco()
    {
        // 32 bytes (256 bits) de entropia - não é JWT porque um refresh
        // token não precisa ser auto-descritível, só imprevisível. Só o
        // hash é persistido (mesmo raciocínio de nunca guardar senha em
        // texto puro), mas sem custo de PBKDF2: o token já nasce
        // aleatório e de alta entropia, diferente de senha escolhida por
        // humano - SHA-256 simples já é suficiente contra força bruta.
        var bytes = RandomNumberGenerator.GetBytes(32);
        var tokenBruto = Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        return (tokenBruto, HashDoToken(tokenBruto));
    }

    private static string HashDoToken(string tokenBruto) =>
        Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(tokenBruto)));
}
