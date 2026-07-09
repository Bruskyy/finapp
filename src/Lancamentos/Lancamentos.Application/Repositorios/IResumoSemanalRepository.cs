using Lancamentos.Application.Relatorios;

namespace Lancamentos.Application.Repositorios;

public interface IResumoSemanalRepository
{
    Task<DateTime?> ObterUltimaGeracaoAsync(Guid usuarioId, CancellationToken ct);

    /// <summary>Grava o cooldown e enfileira o evento na outbox no mesmo SaveChanges - atômico.</summary>
    Task RegistrarGeracaoEEnfileirarAsync(Guid usuarioId, ResumoSemanalCalculado resumo, CancellationToken ct);
}
