namespace Gamificacao.Api.Dominio;

/// <summary>
/// Sequência de dias consecutivos de uso do Cofrin (BACKLOG-PRODUTO.md,
/// Roadmap 1.0, Sprint 2) — alimentada pelo mesmo evento lancamento.criado
/// que já move o ledger de moedas e as conquistas. "Dia de uso" é o dia
/// local (America/Sao_Paulo) do OcorreuEm do evento, não a Data de negócio
/// do lançamento (que pode ser retroativa) — assim lançar hoje uma despesa
/// de a semana passada não "conserta" a sequência artificialmente.
/// </summary>
public class SequenciaUsuario
{
    public Guid UsuarioId { get; private set; }
    public int DiasConsecutivos { get; private set; }
    public int MelhorSequencia { get; private set; }
    public DateOnly UltimoDiaContado { get; private set; }

    private SequenciaUsuario() { }

    public SequenciaUsuario(Guid usuarioId, DateOnly dia)
    {
        UsuarioId = usuarioId;
        DiasConsecutivos = 1;
        MelhorSequencia = 1;
        UltimoDiaContado = dia;
    }

    /// <summary>
    /// Registra uso no dia informado. Idempotente dentro do mesmo dia (vários
    /// lançamentos no mesmo dia não inflam a sequência); o dia seguinte ao
    /// último contado incrementa, qualquer lacuna maior reinicia em 1. Eventos
    /// fora de ordem (dia anterior ao já contado) são ignorados — a fila
    /// entrega at-least-once mas não garante ordem estrita de entrega.
    /// </summary>
    public void RegistrarUso(DateOnly dia)
    {
        if (dia <= UltimoDiaContado) return;

        DiasConsecutivos = dia == UltimoDiaContado.AddDays(1) ? DiasConsecutivos + 1 : 1;
        MelhorSequencia = Math.Max(MelhorSequencia, DiasConsecutivos);
        UltimoDiaContado = dia;
    }
}
