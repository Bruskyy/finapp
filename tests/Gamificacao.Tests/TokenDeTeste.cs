using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Gamificacao.Tests;

/// <summary>
/// Gamificacao.Api não emite token (quem emite é Usuarios.Api) - pros testes
/// que passam pelo pipeline HTTP (WebApplicationFactory), monta um JWT válido
/// na mão com a mesma chave/issuer/audience fixos de teste usados em
/// CriarFactory. Mesmo formato de claims do JwtTokenGenerator real.
/// </summary>
public static class TokenDeTeste
{
    public const string SecretKey = "chave-de-teste-para-ci-com-pelo-menos-32-bytes-0000";

    public static string Gerar(Guid? usuarioId = null)
    {
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, (usuarioId ?? Guid.NewGuid()).ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: "FinApp",
            audience: "FinApp.Clientes",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: credenciais);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
