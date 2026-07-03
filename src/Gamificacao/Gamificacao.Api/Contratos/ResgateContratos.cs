namespace Gamificacao.Api.Contratos;

public record ResgateRequest(int Quantidade);

public record ResgateResponse(Guid Id, int Quantidade, string Status);
