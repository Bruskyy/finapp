namespace BuildingBlocks.Contracts.Gamificacao;

public record ResgateSolicitadoEvent(
    Guid ResgateId,
    int Quantidade,
    DateTime OcorreuEm,
    Guid? UsuarioId = null);

public record ResgateConfirmadoEvent(
    Guid ResgateId,
    DateTime OcorreuEm,
    Guid? UsuarioId = null);

public record ResgateFalhouEvent(
    Guid ResgateId,
    string Motivo,
    DateTime OcorreuEm,
    Guid? UsuarioId = null);
