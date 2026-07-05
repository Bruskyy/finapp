using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Usuarios.Api.Dominio;

namespace Usuarios.Api.Aplicacao;

public class JwtTokenGenerator
{
    private readonly IConfiguration _configuracao;

    public JwtTokenGenerator(IConfiguration configuracao)
    {
        _configuracao = configuracao;
    }

    public string GerarToken(Usuario usuario)
    {
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuracao["Jwt:SecretKey"]!));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            new Claim("name", usuario.Nome),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _configuracao["Jwt:Issuer"],
            audience: _configuracao["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_configuracao.GetValue<int>("Jwt:ExpiracaoMinutos")),
            signingCredentials: credenciais);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
