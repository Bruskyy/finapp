using Gamificacao.Api.Dominio;

namespace Gamificacao.Tests;

public class SequenciaUsuarioTests
{
    [Fact]
    public void Construtor_ComeçaComUmDiaConsecutivo()
    {
        var sequencia = new SequenciaUsuario(Guid.NewGuid(), new DateOnly(2026, 7, 1));

        Assert.Equal(1, sequencia.DiasConsecutivos);
        Assert.Equal(1, sequencia.MelhorSequencia);
        Assert.Equal(new DateOnly(2026, 7, 1), sequencia.UltimoDiaContado);
    }

    [Fact]
    public void RegistrarUso_DiaSeguinte_Incrementa()
    {
        var sequencia = new SequenciaUsuario(Guid.NewGuid(), new DateOnly(2026, 7, 1));

        sequencia.RegistrarUso(new DateOnly(2026, 7, 2));

        Assert.Equal(2, sequencia.DiasConsecutivos);
        Assert.Equal(2, sequencia.MelhorSequencia);
    }

    [Fact]
    public void RegistrarUso_MesmoDia_NaoIncrementa()
    {
        var sequencia = new SequenciaUsuario(Guid.NewGuid(), new DateOnly(2026, 7, 1));

        sequencia.RegistrarUso(new DateOnly(2026, 7, 1));
        sequencia.RegistrarUso(new DateOnly(2026, 7, 1));

        Assert.Equal(1, sequencia.DiasConsecutivos);
    }

    [Fact]
    public void RegistrarUso_ComLacuna_ReiniciaEmUm()
    {
        var sequencia = new SequenciaUsuario(Guid.NewGuid(), new DateOnly(2026, 7, 1));
        sequencia.RegistrarUso(new DateOnly(2026, 7, 2));
        sequencia.RegistrarUso(new DateOnly(2026, 7, 3));

        sequencia.RegistrarUso(new DateOnly(2026, 7, 5)); // pulou o dia 4

        Assert.Equal(1, sequencia.DiasConsecutivos);
    }

    [Fact]
    public void RegistrarUso_ComLacuna_PreservaAMelhorSequenciaJaAtingida()
    {
        var sequencia = new SequenciaUsuario(Guid.NewGuid(), new DateOnly(2026, 7, 1));
        sequencia.RegistrarUso(new DateOnly(2026, 7, 2));
        sequencia.RegistrarUso(new DateOnly(2026, 7, 3)); // melhor sequência = 3

        sequencia.RegistrarUso(new DateOnly(2026, 7, 10)); // reinicia em 1

        Assert.Equal(1, sequencia.DiasConsecutivos);
        Assert.Equal(3, sequencia.MelhorSequencia);
    }

    [Fact]
    public void RegistrarUso_DiaAnteriorAoJaContado_EIgnorado()
    {
        var sequencia = new SequenciaUsuario(Guid.NewGuid(), new DateOnly(2026, 7, 5));

        sequencia.RegistrarUso(new DateOnly(2026, 7, 3)); // evento fora de ordem

        Assert.Equal(1, sequencia.DiasConsecutivos);
        Assert.Equal(new DateOnly(2026, 7, 5), sequencia.UltimoDiaContado);
    }
}
