namespace Gamificacao.Api.Dominio;

/// <summary>
/// Contador de marcos por usuário (ex: quantos lançamentos, quantas metas
/// concluídas) - tabela dedicada em vez de COUNT(*) em MovimentosMoedas, que
/// exigiria casar por texto no campo Motivo (frágil) pra distinguir os tipos
/// de evento. Chave composta (UsuarioId, Chave); incremento O(1).
/// </summary>
public class ContadorConquista
{
    public Guid UsuarioId { get; private set; }
    public string Chave { get; private set; }
    public int Valor { get; private set; }

    private ContadorConquista() { Chave = null!; }

    public ContadorConquista(Guid usuarioId, string chave)
    {
        UsuarioId = usuarioId;
        Chave = chave;
        Valor = 0;
    }

    public void Incrementar() => Valor++;
}
