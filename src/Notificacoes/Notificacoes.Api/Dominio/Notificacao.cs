namespace Notificacoes.Api.Dominio;

public enum TipoNotificacao
{
    Lancamento = 1,
    LancamentoRecorrente = 2,
    ResgateConfirmado = 3,
    ResgateFalhou = 4,
}

public class Notificacao
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public TipoNotificacao Tipo { get; private set; }
    public string Mensagem { get; private set; }
    public bool Lida { get; private set; }
    public DateTime CriadoEm { get; private set; }

    private Notificacao() { Mensagem = null!; }

    public Notificacao(Guid eventId, TipoNotificacao tipo, string mensagem, Guid? usuarioId)
    {
        if (string.IsNullOrWhiteSpace(mensagem))
            throw new ArgumentException("Mensagem é obrigatória.", nameof(mensagem));

        Id = Guid.NewGuid();
        EventId = eventId;
        Tipo = tipo;
        Mensagem = mensagem.Trim();
        UsuarioId = usuarioId;
        Lida = false;
        CriadoEm = DateTime.UtcNow;
    }

    public void MarcarComoLida() => Lida = true;
}
