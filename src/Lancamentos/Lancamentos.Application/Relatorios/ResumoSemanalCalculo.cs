using Lancamentos.Domain.Entidades;

namespace Lancamentos.Application.Relatorios;

/// <summary>
/// Lógica pura de montagem do resumo semanal (BACKLOG-PRODUTO.md, Onda 1,
/// item 4) — recebe os agregados já calculados (via repositórios) e monta
/// o resultado, sem I/O. Testável com listas em memória, sem banco.
/// </summary>
public static class ResumoSemanalCalculo
{
    public static ResumoSemanalCalculado Montar(
        decimal saldoJanelaAtual,
        decimal saldoJanelaAnterior,
        IReadOnlyList<GastoPorCategoria> gastosCategoria,
        int diasComLancamento,
        IReadOnlyList<Objetivo> objetivos)
    {
        var categoriaTop = gastosCategoria.OrderByDescending(g => g.TotalGasto).FirstOrDefault();

        // mesma regra de 2 linhas já usada no app (DashboardScreen.tsx) pra
        // decidir "qual meta é a destaque": não concluída, maior percentual.
        var destaque = objetivos
            .Where(o => !o.Concluido)
            .OrderByDescending(o => o.PercentualConcluido)
            .FirstOrDefault();

        return new ResumoSemanalCalculado(
            EconomiaVsSemanaAnterior: saldoJanelaAtual - saldoJanelaAnterior,
            CategoriaMaiorGasto: categoriaTop?.Categoria,
            ValorCategoriaMaiorGasto: categoriaTop?.TotalGasto ?? 0,
            DiasComLancamento: diasComLancamento,
            NomeObjetivoDestaque: destaque?.Nome,
            PercentualObjetivoDestaque: destaque?.PercentualConcluido);
    }
}
