namespace Gamificacao.Api.Regras;

/// <summary>Chaves de negócio estáveis do catálogo de conquistas - usadas em código, independentes do Guid gerado no seed da migration.</summary>
public static class ConquistaCodigos
{
    public const string PrimeiroSalario = "PRIMEIRO_SALARIO";
    public const string Lancamentos1 = "LANCAMENTOS_1";
    public const string Lancamentos10 = "LANCAMENTOS_10";
    public const string Lancamentos50 = "LANCAMENTOS_50";
    public const string Lancamentos100 = "LANCAMENTOS_100";
    public const string Lancamentos500 = "LANCAMENTOS_500";
    public const string Lancamentos1000 = "LANCAMENTOS_1000";
    public const string PrimeiraMetaConcluida = "PRIMEIRA_META_CONCLUIDA";
    public const string MetasConcluidas5 = "METAS_CONCLUIDAS_5";
    public const string MetasConcluidas10 = "METAS_CONCLUIDAS_10";
    public const string MetasConcluidas25 = "METAS_CONCLUIDAS_25";
    // Consistência (Roadmap 1.0, Sprint 2) - alimentadas por SequenciaService,
    // não por ConquistaService (contador de dias, não de eventos).
    public const string Sequencia7 = "SEQUENCIA_7";
    public const string Sequencia30 = "SEQUENCIA_30";
    public const string Sequencia100 = "SEQUENCIA_100";
    public const string Sequencia365 = "SEQUENCIA_365";
}
