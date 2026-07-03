using System.Text.Json;
using BuildingBlocks.Contracts.Gamificacao;
using Gamificacao.Api.Dominio;
using Gamificacao.Api.Persistencia;

namespace Gamificacao.Api.Aplicacao;

public class ResgateService
{
    private readonly GamificacaoDbContext _db;
    private readonly IMovimentoMoedasRepository _movimentos;

    public ResgateService(GamificacaoDbContext db, IMovimentoMoedasRepository movimentos)
    {
        _db = db;
        _movimentos = movimentos;
    }

    public async Task<Resgate> SolicitarAsync(int quantidade, CancellationToken ct)
    {
        var saldo = await _movimentos.ObterSaldoAsync(ct);
        if (quantidade > saldo)
            throw new SaldoInsuficienteException(quantidade, saldo);

        var resgate = new Resgate(quantidade);

        // reserva as moedas debitando imediatamente; se a saga falhar, o consumidor de
        // resultado compensa (credita de volta) - ver ResgateResultadoConsumerService
        var debito = new MovimentoMoedas(resgate.Id, quantidade, TipoMovimento.Debito, $"Reserva do resgate {resgate.Id}");

        var evento = new ResgateSolicitadoEvent(resgate.Id, quantidade, DateTime.UtcNow);
        var mensagem = new OutboxMessage(nameof(ResgateSolicitadoEvent), JsonSerializer.Serialize(evento));

        _db.Resgates.Add(resgate);
        _db.Movimentos.Add(debito);
        _db.OutboxMessages.Add(mensagem);

        await _db.SaveChangesAsync(ct);

        return resgate;
    }
}
