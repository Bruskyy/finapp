namespace Gamificacao.Api.Aplicacao;

public class SaldoInsuficienteException : Exception
{
    public SaldoInsuficienteException(int quantidadeSolicitada, int saldoDisponivel)
        : base($"Saldo insuficiente: solicitado {quantidadeSolicitada}, disponível {saldoDisponivel}.")
    {
    }
}
