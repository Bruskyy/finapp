namespace Gamificacao.Api.Contratos;

public record ConquistaResponse(
    Guid Id,
    string Codigo,
    string Nome,
    string Descricao,
    string Icone,
    DateTime? DesbloqueadaEm);
