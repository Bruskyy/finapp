namespace Lancamentos.Domain.Entidades;

/// <summary>
/// Meta de poupança (estilo Mobills): "Viagem R$ 5.000 até dezembro".
/// O progresso avança por aportes; ao atingir o alvo, o objetivo conclui
/// (e a Gamificação credita bônus via evento objetivo.concluido).
/// </summary>
public class Objetivo
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public decimal ValorAlvo { get; private set; }
    public DateTime DataAlvo { get; private set; }
    public decimal ValorAcumulado { get; private set; }
    public bool Concluido { get; private set; }
    // Nulo até o aporte que concluiu a meta (ver Aportar) - metas
    // concluídas antes desta coluna existir ficam sem essa data, sem
    // backfill possível (a informação nunca foi registrada).
    public DateTime? ConcluidoEm { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public DateTime CriadoEm { get; private set; }

    private Objetivo() { Nome = null!; }

    public Objetivo(string nome, decimal valorAlvo, DateTime dataAlvo, Guid usuarioId, DateTime? hoje = null)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome é obrigatório.", nameof(nome));
        if (valorAlvo <= 0)
            throw new ArgumentException("Valor alvo deve ser maior que zero.", nameof(valorAlvo));
        if (dataAlvo.Date <= (hoje ?? DateTime.Today).Date)
            throw new ArgumentException("Data alvo deve ser no futuro.", nameof(dataAlvo));

        Id = Guid.NewGuid();
        Nome = nome.Trim();
        ValorAlvo = valorAlvo;
        DataAlvo = dataAlvo.Date;
        ValorAcumulado = 0;
        Concluido = false;
        UsuarioId = usuarioId;
        CriadoEm = DateTime.UtcNow;
    }

    /// <summary>Registra um aporte; retorna true se este aporte concluiu o objetivo.</summary>
    public bool Aportar(decimal valor)
    {
        if (valor <= 0)
            throw new ArgumentException("Valor do aporte deve ser maior que zero.", nameof(valor));
        if (Concluido)
            throw new InvalidOperationException($"Objetivo '{Nome}' já foi concluído.");

        ValorAcumulado += valor;

        if (ValorAcumulado >= ValorAlvo)
        {
            Concluido = true;
            ConcluidoEm = DateTime.UtcNow;
            return true;
        }

        return false;
    }

    /// <summary>Percentual de conclusão (0–100, limitado a 100).</summary>
    public decimal PercentualConcluido =>
        Math.Min(100, Math.Round(ValorAcumulado / ValorAlvo * 100, 1));

    /// <summary>
    /// Simulador do Mobills: quanto guardar por mês para atingir o alvo até a
    /// data. Meses contados de forma inclusiva a partir do mês seguinte; se a
    /// data alvo já passou (objetivo atrasado), retorna o que falta de uma vez.
    /// Lógica pura de domínio — testável sem banco/relogio real.
    /// </summary>
    public decimal ValorMensalNecessario(DateTime hoje)
    {
        var falta = ValorAlvo - ValorAcumulado;
        if (falta <= 0)
            return 0;

        var mesesRestantes = (DataAlvo.Year - hoje.Year) * 12 + (DataAlvo.Month - hoje.Month);
        if (mesesRestantes <= 0)
            return Math.Round(falta, 2);

        return Math.Round(falta / mesesRestantes, 2);
    }

    /// <summary>
    /// Projeção de quando a meta fica pronta no RITMO ATUAL de aportes
    /// (taxa média desde a criação) — diferente de
    /// <see cref="ValorMensalNecessario"/>, que calcula quanto FALTARIA
    /// guardar por mês pra bater o prazo definido. É essa diferença entre
    /// "ritmo necessário" (normativo) e "ritmo real projetado" (descritivo)
    /// que dá o "adianta/atrasa" mostrado no app. Null se ainda não há
    /// ritmo pra projetar (nenhum aporte feito).
    /// </summary>
    public DateTime? PrevisaoConclusaoEm(DateTime hoje)
    {
        if (Concluido)
            return ConcluidoEm;
        if (ValorAcumulado <= 0)
            return null;

        var diasDecorridos = Math.Max(1, (hoje.Date - CriadoEm.Date).Days);
        var ritmoMensal = ValorAcumulado / diasDecorridos * 30;
        if (ritmoMensal <= 0)
            return null;

        var falta = ValorAlvo - ValorAcumulado;
        // Arredonda antes do Ceiling: a divisão decimal de "falta/ritmoMensal"
        // frequentemente não fecha num número exato de dias (ex: 4000/3000*30
        // vira 39,999999999999999999999999996... por causa da dízima
        // periódica truncada em 4/3) - sem esse arredondamento, o Ceiling
        // arredondaria pra cima indevidamente e a previsão saía um dia adiantada.
        var diasRestantes = Math.Round(falta / ritmoMensal * 30, 6);
        return hoje.Date.AddDays((double)Math.Ceiling(diasRestantes));
    }
}
