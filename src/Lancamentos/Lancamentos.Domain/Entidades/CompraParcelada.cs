namespace Lancamentos.Domain.Entidades;

/// <summary>
/// Compra-mãe de um parcelamento no cartão (ITEM-CARTAO-CREDITO.md, decisão
/// 3): as N parcelas são lançamentos comuns vinculados a ela, gerados TODOS
/// no ato da compra (atomicidade da transação local, sem worker) com
/// competências consecutivas. A divisão do valor é lógica pura testável sem
/// banco - ajuste de centavos na primeira parcela.
/// </summary>
public class CompraParcelada
{
    public Guid Id { get; private set; }
    public string Descricao { get; private set; }
    public decimal ValorTotal { get; private set; }
    public int NumeroParcelas { get; private set; }
    public Guid ContaId { get; private set; }
    public Guid CategoriaId { get; private set; }
    public DateTime DataCompra { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public DateTime CriadoEm { get; private set; }

    private CompraParcelada() { Descricao = null!; }

    public CompraParcelada(string descricao, decimal valorTotal, int numeroParcelas, Guid contaId, Guid categoriaId, DateTime dataCompra, Guid usuarioId)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("Descrição é obrigatória.", nameof(descricao));
        if (valorTotal <= 0)
            throw new ArgumentException("Valor total deve ser maior que zero.", nameof(valorTotal));
        if (numeroParcelas is < 2 or > 48)
            throw new ArgumentException("Número de parcelas deve estar entre 2 e 48.", nameof(numeroParcelas));

        Id = Guid.NewGuid();
        Descricao = descricao.Trim();
        ValorTotal = valorTotal;
        NumeroParcelas = numeroParcelas;
        ContaId = contaId;
        CategoriaId = categoriaId;
        DataCompra = dataCompra;
        UsuarioId = usuarioId;
        CriadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Materializa as N parcelas como lançamentos de despesa no cartão:
    /// "Descricao (i/N)", competências consecutivas a partir da regra de
    /// fechamento do cartão, e a Data de cada parcela cai no mês da própria
    /// competência (relatórios de caixa por Data continuam fazendo sentido -
    /// decisão 4b). Divisão: parcela-base arredondada pra baixo no centavo e
    /// a primeira leva a sobra, então a soma bate exatamente com o total.
    /// </summary>
    public IReadOnlyList<Lancamento> GerarParcelas(Conta cartao)
    {
        if (cartao.Id != ContaId)
            throw new InvalidOperationException("Conta informada não é a conta da compra.");
        if (!cartao.EhCartao)
            throw new InvalidOperationException("Compra parcelada só existe em cartão de crédito.");

        var valorBase = Math.Floor(ValorTotal / NumeroParcelas * 100m) / 100m;
        var primeiraParcela = ValorTotal - valorBase * (NumeroParcelas - 1);
        var primeiraCompetencia = cartao.CompetenciaPara(DataCompra)!.Value;

        var parcelas = new List<Lancamento>(NumeroParcelas);
        for (var i = 0; i < NumeroParcelas; i++)
        {
            var competencia = primeiraCompetencia.AddMonths(i);
            // 1ª parcela mantém a data real da compra; as demais caem no mesmo
            // dia (clampado ao fim do mês) do mês da sua competência.
            var data = i == 0
                ? DataCompra
                : new DateTime(competencia.Year, competencia.Month,
                    Math.Min(DataCompra.Day, DateTime.DaysInMonth(competencia.Year, competencia.Month)));

            parcelas.Add(Lancamento.CriarParcela(
                $"{Descricao} ({i + 1}/{NumeroParcelas})",
                i == 0 ? primeiraParcela : valorBase,
                CategoriaId,
                ContaId,
                data,
                competencia,
                Id,
                i + 1,
                UsuarioId));
        }

        return parcelas;
    }
}
