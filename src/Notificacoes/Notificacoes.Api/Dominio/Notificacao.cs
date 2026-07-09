namespace Notificacoes.Api.Dominio;

public enum TipoNotificacao
{
    Lancamento = 1,
    LancamentoRecorrente = 2,
    ResgateConfirmado = 3,
    ResgateFalhou = 4,
    ResumoSemanal = 5,
    OrcamentoEstourado = 6,
    RecorrenciaAVencer = 7,
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

    // Dados estruturados do resumo semanal - só preenchidos quando
    // Tipo == ResumoSemanal. Colunas esparsas aceitas deliberadamente (mesma
    // decisão já tomada pro perfil de onboarding em Usuario) em vez de uma
    // tabela separada por tipo, que adicionaria complexidade sem necessidade
    // clara agora - ver README, "Resumo semanal determinístico".
    public decimal? EconomiaVsSemanaAnterior { get; private set; }
    public string? CategoriaMaiorGasto { get; private set; }
    public decimal? ValorCategoriaMaiorGasto { get; private set; }
    public int? DiasComLancamento { get; private set; }
    public string? NomeObjetivoDestaque { get; private set; }
    public decimal? PercentualObjetivoDestaque { get; private set; }

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

    public static Notificacao ParaResumoSemanal(
        Guid eventId,
        string mensagem,
        Guid? usuarioId,
        decimal economiaVsSemanaAnterior,
        string? categoriaMaiorGasto,
        decimal valorCategoriaMaiorGasto,
        int diasComLancamento,
        string? nomeObjetivoDestaque,
        decimal? percentualObjetivoDestaque)
    {
        var notificacao = new Notificacao(eventId, TipoNotificacao.ResumoSemanal, mensagem, usuarioId)
        {
            EconomiaVsSemanaAnterior = economiaVsSemanaAnterior,
            CategoriaMaiorGasto = categoriaMaiorGasto,
            ValorCategoriaMaiorGasto = valorCategoriaMaiorGasto,
            DiasComLancamento = diasComLancamento,
            NomeObjetivoDestaque = nomeObjetivoDestaque,
            PercentualObjetivoDestaque = percentualObjetivoDestaque,
        };
        return notificacao;
    }

    public void MarcarComoLida() => Lida = true;
}
