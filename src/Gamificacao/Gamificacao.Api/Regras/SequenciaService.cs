using Gamificacao.Api.Persistencia;

namespace Gamificacao.Api.Regras;

/// <summary>
/// Registra o uso diário do usuário (streak) e avalia as conquistas de
/// consistência associadas — mesmo papel arquitetural de ConquistaService,
/// mas com estado próprio (SequenciaUsuario) em vez de um contador simples,
/// porque "sequência" tem lógica de reset/continuação que um contador
/// puramente incremental não tem.
/// </summary>
public class SequenciaService
{
    // IANA (não Windows) — resolvido pelo ICU do .NET em qualquer SO, inclusive
    // Linux (CI, Docker, Render), onde o serviço roda de verdade.
    private static readonly TimeZoneInfo FusoBrasil = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    private readonly ISequenciaRepository _sequencias;
    private readonly IConquistaRepository _conquistas;

    public SequenciaService(ISequenciaRepository sequencias, IConquistaRepository conquistas)
    {
        _sequencias = sequencias;
        _conquistas = conquistas;
    }

    /// <summary>
    /// "Dia de uso" é o dia local (America/Sao_Paulo) do instante em que o
    /// evento ocorreu (OcorreuEm) — não a data de negócio do lançamento, que
    /// pode ser retroativa e não reflete quando o usuário de fato abriu o app.
    /// </summary>
    public async Task RegistrarUsoAsync(Guid usuarioId, DateTime ocorreuEmUtc, CancellationToken ct)
    {
        var dia = DiaLocal(ocorreuEmUtc);
        var sequencia = await _sequencias.RegistrarUsoAsync(usuarioId, dia, ct);

        var conquista = ConquistaThresholds.ParaSequencia(sequencia.DiasConsecutivos);
        if (conquista is not null)
            await _conquistas.DesbloquearAsync(usuarioId, conquista, ct);
    }

    private static DateOnly DiaLocal(DateTime ocorreuEmUtc)
    {
        var utc = DateTime.SpecifyKind(ocorreuEmUtc, DateTimeKind.Utc);
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, FusoBrasil);
        return DateOnly.FromDateTime(local);
    }
}
