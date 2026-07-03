using System.Security.Cryptography;
using System.Text;

namespace Gamificacao.Api.Dominio;

public static class IdempotenciaHelper
{
    /// <summary>
    /// Deriva um EventId determinístico a partir de um id de correlação e um sufixo,
    /// de forma que reprocessar o mesmo evento sempre gere o mesmo EventId
    /// (usado para dar idempotência a compensações, sem precisar de tabela extra).
    /// </summary>
    public static Guid DerivarEventId(Guid origem, string sufixo)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes($"{origem}:{sufixo}"));
        return new Guid(hash);
    }
}
