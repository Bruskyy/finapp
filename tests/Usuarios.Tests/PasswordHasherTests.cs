using Microsoft.AspNetCore.Identity;
using Usuarios.Api.Dominio;

namespace Usuarios.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void HashPassword_ComAMesmaSenhaDuasVezes_DeveGerarHashesDiferentes()
    {
        var hasher = new PasswordHasher<Usuario>();

        var hash1 = hasher.HashPassword(null!, "Senha123!");
        var hash2 = hasher.HashPassword(null!, "Senha123!");

        // salt aleatório por chamada - mesma senha nunca produz o mesmo hash,
        // o que impede um ataque de rainbow table comparando hashes iguais.
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyHashedPassword_ComSenhaCorreta_DeveRetornarSuccess()
    {
        var hasher = new PasswordHasher<Usuario>();
        var hash = hasher.HashPassword(null!, "Senha123!");

        var resultado = hasher.VerifyHashedPassword(null!, hash, "Senha123!");

        Assert.Equal(PasswordVerificationResult.Success, resultado);
    }

    [Fact]
    public void VerifyHashedPassword_ComSenhaErrada_DeveRetornarFailed()
    {
        var hasher = new PasswordHasher<Usuario>();
        var hash = hasher.HashPassword(null!, "Senha123!");

        var resultado = hasher.VerifyHashedPassword(null!, hash, "senhaerrada");

        Assert.Equal(PasswordVerificationResult.Failed, resultado);
    }
}
