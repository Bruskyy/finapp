namespace Gamificacao.Api.Regras;

/// <summary>
/// Lógica pura de "essa contagem bate um marco?" - sem I/O, testável sem
/// banco (mesmo espírito de CalculadoraDePontuacao, mas pra thresholds em
/// vez de regra única por tipo).
/// </summary>
public static class ConquistaThresholds
{
    public static string? ParaLancamentos(int contagem) => contagem switch
    {
        10 => ConquistaCodigos.Lancamentos10,
        100 => ConquistaCodigos.Lancamentos100,
        1000 => ConquistaCodigos.Lancamentos1000,
        _ => null,
    };

    public static string? ParaMetasConcluidas(int contagem) => contagem switch
    {
        5 => ConquistaCodigos.MetasConcluidas5,
        _ => null,
    };
}
