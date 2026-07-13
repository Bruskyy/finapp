namespace Usuarios.Api.Dominio;

/// <summary>
/// Rastreio de cooldown do convite de apoio (BACKLOG-PRODUTO.md, Sprint 7) -
/// mesmo padrão de ResumoSemanalGerado (Lancamentos): uma linha por usuário
/// (upsert), não histórico. Controla só "quando foi o último convite",
/// pra sustentar a régua "primeira vez aos 30 dias de uso, depois só a
/// cada alguns meses se ignorado - nunca semanal/mensal".
/// </summary>
public class ApoioNotificado
{
    public Guid UsuarioId { get; private set; }
    public DateTime UltimoEnvioEm { get; private set; }

    private ApoioNotificado() { }

    public ApoioNotificado(Guid usuarioId, DateTime enviadoEm)
    {
        UsuarioId = usuarioId;
        UltimoEnvioEm = enviadoEm;
    }

    public void AtualizarEnvio(DateTime enviadoEm) => UltimoEnvioEm = enviadoEm;
}
