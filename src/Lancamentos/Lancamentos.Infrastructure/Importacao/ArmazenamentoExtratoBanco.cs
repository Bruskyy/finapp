using Lancamentos.Application.Importacao;
using Lancamentos.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Infrastructure.Importacao;

/// <summary>
/// Adapter de armazenamento pro deploy (Importacoes:Modo = "Banco"): guarda o
/// CSV (≤ 1 MB, limite validado no endpoint) numa tabela do próprio SQL Server,
/// no lugar do S3 — o ambiente deployado (Render) não tem LocalStack nem AWS.
/// Mesma porta, outra tecnologia: a Application não percebe a troca.
/// </summary>
public class ArmazenamentoExtratoBanco : IArmazenamentoExtrato
{
    private readonly LancamentosDbContext _db;

    public ArmazenamentoExtratoBanco(LancamentosDbContext db)
    {
        _db = db;
    }

    public async Task SalvarAsync(string chave, string conteudo, CancellationToken ct)
    {
        _db.ExtratosArquivos.Add(new ExtratoArquivo(chave, conteudo));
        await _db.SaveChangesAsync(ct);
    }

    public async Task<string> BaixarAsync(string chave, CancellationToken ct)
    {
        var arquivo = await _db.ExtratosArquivos.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Chave == chave, ct)
            ?? throw new InvalidOperationException($"Extrato '{chave}' não encontrado no armazenamento.");

        return arquivo.Conteudo;
    }

    // A tabela nasce por migration no boot — nada a preparar aqui.
    public Task GarantirInfraestruturaAsync(CancellationToken ct) => Task.CompletedTask;
}
