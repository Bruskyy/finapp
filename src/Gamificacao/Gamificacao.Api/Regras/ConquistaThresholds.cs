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
        1 => ConquistaCodigos.Lancamentos1,
        10 => ConquistaCodigos.Lancamentos10,
        50 => ConquistaCodigos.Lancamentos50,
        100 => ConquistaCodigos.Lancamentos100,
        500 => ConquistaCodigos.Lancamentos500,
        1000 => ConquistaCodigos.Lancamentos1000,
        _ => null,
    };

    public static string? ParaMetasConcluidas(int contagem) => contagem switch
    {
        5 => ConquistaCodigos.MetasConcluidas5,
        10 => ConquistaCodigos.MetasConcluidas10,
        25 => ConquistaCodigos.MetasConcluidas25,
        _ => null,
    };

    public static string? ParaSequencia(int dias) => dias switch
    {
        7 => ConquistaCodigos.Sequencia7,
        30 => ConquistaCodigos.Sequencia30,
        100 => ConquistaCodigos.Sequencia100,
        365 => ConquistaCodigos.Sequencia365,
        _ => null,
    };
}
