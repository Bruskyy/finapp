namespace Gamificacao.Api.Dominio;

public enum StatusResgate
{
    Pendente = 1,
    Confirmado = 2,
    Compensado = 3
}

public class Resgate
{
    public Guid Id { get; private set; }
    public int Quantidade { get; private set; }
    public StatusResgate Status { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime AtualizadoEm { get; private set; }

    private Resgate() { }

    public Resgate(int quantidade)
    {
        if (quantidade <= 0)
            throw new ArgumentException("Quantidade deve ser maior que zero.", nameof(quantidade));

        Id = Guid.NewGuid();
        Quantidade = quantidade;
        Status = StatusResgate.Pendente;
        CriadoEm = DateTime.UtcNow;
        AtualizadoEm = CriadoEm;
    }

    public void Confirmar()
    {
        if (Status != StatusResgate.Pendente)
            throw new InvalidOperationException($"Resgate {Id} não está pendente (status atual: {Status}).");

        Status = StatusResgate.Confirmado;
        AtualizadoEm = DateTime.UtcNow;
    }

    public void Compensar()
    {
        if (Status != StatusResgate.Pendente)
            throw new InvalidOperationException($"Resgate {Id} não está pendente (status atual: {Status}).");

        Status = StatusResgate.Compensado;
        AtualizadoEm = DateTime.UtcNow;
    }
}
