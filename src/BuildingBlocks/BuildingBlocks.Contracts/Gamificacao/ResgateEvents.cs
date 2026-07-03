namespace BuildingBlocks.Contracts.Gamificacao;

public record ResgateSolicitadoEvent(
    Guid ResgateId,
    int Quantidade,
    DateTime OcorreuEm);

public record ResgateConfirmadoEvent(
    Guid ResgateId,
    DateTime OcorreuEm);

public record ResgateFalhouEvent(
    Guid ResgateId,
    string Motivo,
    DateTime OcorreuEm);
