namespace Lancamentos.Application.Orcamentos;

/// <summary>
/// Lógica pura de "quais limiares esse percentual cruzou" - sem I/O,
/// testável sem banco. Se o gasto já passou de 100% de uma vez (ex:
/// lançamento grande numa categoria sem histórico), os dois limiares
/// disparam juntos - comportamento correto, não um bug.
/// </summary>
public static class OrcamentoAlertaRegras
{
    private static readonly int[] Limiares = [80, 100];

    public static IReadOnlyList<int> LimiaresParaAlertar(decimal percentualUsado) =>
        Limiares.Where(limiar => percentualUsado >= limiar).ToList();
}
