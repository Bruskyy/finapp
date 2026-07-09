namespace Gamificacao.Api.Dominio;

/// <summary>
/// Registro de desbloqueio de uma <see cref="Conquista"/> por um usuário -
/// índice único em (UsuarioId, ConquistaId) garante idempotência (mesmo
/// padrão de MovimentosMoedas.EventId).
/// </summary>
public class UsuarioConquista
{
    public Guid Id { get; private set; }
    public Guid ConquistaId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public DateTime DesbloqueadaEm { get; private set; }

    private UsuarioConquista() { }

    public UsuarioConquista(Guid conquistaId, Guid usuarioId)
    {
        Id = Guid.NewGuid();
        ConquistaId = conquistaId;
        UsuarioId = usuarioId;
        DesbloqueadaEm = DateTime.UtcNow;
    }
}
