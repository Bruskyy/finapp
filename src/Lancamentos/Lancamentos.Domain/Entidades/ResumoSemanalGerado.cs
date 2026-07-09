namespace Lancamentos.Domain.Entidades;

/// <summary>
/// Rastreio mínimo de cooldown do ResumoSemanalWorker - não é o resumo em
/// si (esse vai inteiro pro evento/notificação), só controla "já gerei
/// recentemente pra este usuário" via UltimaGeracaoEm. Uma linha por
/// usuário (upsert), não histórico.
/// </summary>
public class ResumoSemanalGerado
{
    public Guid UsuarioId { get; private set; }
    public DateTime UltimaGeracaoEm { get; private set; }

    private ResumoSemanalGerado() { }

    public ResumoSemanalGerado(Guid usuarioId, DateTime geradoEm)
    {
        UsuarioId = usuarioId;
        UltimaGeracaoEm = geradoEm;
    }

    public void AtualizarGeracao(DateTime geradoEm) => UltimaGeracaoEm = geradoEm;
}
