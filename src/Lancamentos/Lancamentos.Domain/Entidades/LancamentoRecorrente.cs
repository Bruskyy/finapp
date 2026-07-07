namespace Lancamentos.Domain.Entidades;

/// <summary>
/// Conta fixa (estilo Mobills): despesa/receita que repete todo mês
/// (aluguel, assinatura, salário). Um worker materializa o lançamento
/// do mês quando o dia chega — ver RecorrenciaWorker.
/// </summary>
public class LancamentoRecorrente
{
    public Guid Id { get; private set; }
    public string Descricao { get; private set; }
    public decimal Valor { get; private set; }
    public TipoLancamento Tipo { get; private set; }
    public Guid CategoriaId { get; private set; }
    public Guid ContaId { get; private set; }
    public Guid? UsuarioId { get; private set; }

    /// <summary>Dia do mês do vencimento (1–31; em meses mais curtos vale o último dia).</summary>
    public int DiaDoMes { get; private set; }

    public bool Ativa { get; private set; }
    public DateTime CriadoEm { get; private set; }

    private LancamentoRecorrente() { Descricao = null!; }

    public LancamentoRecorrente(string descricao, decimal valor, TipoLancamento tipo, Guid categoriaId, Guid contaId, int diaDoMes, Guid usuarioId)
    {
        if (valor <= 0)
            throw new ArgumentException("Valor deve ser maior que zero.", nameof(valor));
        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("Descrição é obrigatória.", nameof(descricao));
        if (contaId == Guid.Empty)
            throw new ArgumentException("Conta é obrigatória.", nameof(contaId));
        if (diaDoMes is < 1 or > 31)
            throw new ArgumentException("Dia do mês deve estar entre 1 e 31.", nameof(diaDoMes));

        Id = Guid.NewGuid();
        Descricao = descricao.Trim();
        Valor = valor;
        Tipo = tipo;
        CategoriaId = categoriaId;
        ContaId = contaId;
        DiaDoMes = diaDoMes;
        UsuarioId = usuarioId;
        Ativa = true;
        CriadoEm = DateTime.UtcNow;
    }

    public void Pausar() => Ativa = false;
    public void Reativar() => Ativa = true;

    /// <summary>
    /// Dia real do vencimento num mês específico: dia 31 numa competência de
    /// fevereiro vira 28/29 (comportamento Mobills para "todo dia 31").
    /// </summary>
    public int DiaEfetivoEm(int ano, int mes) => Math.Min(DiaDoMes, DateTime.DaysInMonth(ano, mes));

    /// <summary>
    /// Se a recorrência está vencida na data de referência (o dia do mês já
    /// chegou/passou) — usada pelo worker, que também confere se a competência
    /// já foi processada (idempotência fica no banco, não aqui).
    /// </summary>
    public bool VencidaEm(DateTime referencia) =>
        Ativa && referencia.Day >= DiaEfetivoEm(referencia.Year, referencia.Month);

    /// <summary>Identificador da competência mensal, ex: "2026-07".</summary>
    public static string CompetenciaDe(DateTime data) => $"{data:yyyy-MM}";

    /// <summary>Materializa o lançamento desta recorrência para uma competência.</summary>
    public Lancamento MaterializarEm(DateTime referencia)
    {
        var dia = DiaEfetivoEm(referencia.Year, referencia.Month);
        var data = new DateTime(referencia.Year, referencia.Month, dia);
        return Lancamento.CriarDeRecorrencia(Descricao, Valor, Tipo, CategoriaId, ContaId, data, Id, UsuarioId);
    }
}
