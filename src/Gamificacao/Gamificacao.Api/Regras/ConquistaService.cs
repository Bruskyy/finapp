using BuildingBlocks.Contracts.Lancamentos;
using Gamificacao.Api.Persistencia;

namespace Gamificacao.Api.Regras;

/// <summary>
/// Orquestra a avaliação de conquistas a partir dos eventos que
/// Gamificacao.Api já consome (BACKLOG-PRODUTO.md, Onda 1, item 5). Não é
/// uma implementação de IRegraPontuacao: aquele Strategy é mutuamente
/// exclusivo (um lançamento gera UM movimento de moedas); aqui o mesmo
/// evento pode disparar VÁRIAS verificações ao mesmo tempo (um lançamento
/// de salário conta tanto pra "primeiro salário" quanto pro contador de
/// lançamentos) - por isso uma classe dedicada, mesmo papel arquitetural de
/// CalculadoraDePontuacao/ResgateService.
/// </summary>
public class ConquistaService
{
    // Categoria "Salário" é dado de referência global fixo, seedado em
    // Lancamentos.Infrastructure (migration OrcamentosECategoriasSeed).
    // Gamificacao.Api não tem acesso ao catálogo de categorias - só recebe
    // o Guid cru no evento - então referencia essa constante diretamente.
    // Trade-off aceito: acoplamento cross-service pontual, mas mais barato
    // que uma chamada síncrona só pra resolver o nome de uma categoria.
    private static readonly Guid CategoriaSalarioId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    private readonly IConquistaRepository _repo;

    public ConquistaService(IConquistaRepository repo)
    {
        _repo = repo;
    }

    public async Task AvaliarLancamentoAsync(Guid usuarioId, Guid categoriaId, TipoLancamento tipo, CancellationToken ct)
    {
        if (tipo == TipoLancamento.Receita && categoriaId == CategoriaSalarioId)
            await _repo.DesbloquearAsync(usuarioId, ConquistaCodigos.PrimeiroSalario, ct);

        var contagem = await _repo.IncrementarContadorAsync(usuarioId, ContadorChaves.Lancamentos, ct);
        var conquista = ConquistaThresholds.ParaLancamentos(contagem);
        if (conquista is not null)
            await _repo.DesbloquearAsync(usuarioId, conquista, ct);
    }

    public async Task AvaliarObjetivoConcluidoAsync(Guid usuarioId, CancellationToken ct)
    {
        await _repo.DesbloquearAsync(usuarioId, ConquistaCodigos.PrimeiraMetaConcluida, ct);

        var contagem = await _repo.IncrementarContadorAsync(usuarioId, ContadorChaves.MetasConcluidas, ct);
        var conquista = ConquistaThresholds.ParaMetasConcluidas(contagem);
        if (conquista is not null)
            await _repo.DesbloquearAsync(usuarioId, conquista, ct);
    }
}
