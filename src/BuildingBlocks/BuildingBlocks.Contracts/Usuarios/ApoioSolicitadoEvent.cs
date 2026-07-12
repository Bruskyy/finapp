namespace BuildingBlocks.Contracts.Usuarios;

/// <summary>
/// Publicado por Usuarios.Api (ApoioWorker) quando um usuário se torna
/// elegível pra ver o convite de apoio (BACKLOG-PRODUTO.md, Sprint 7) -
/// consumido por Notificacoes.Api, que gera a notificação/push. Primeira
/// vez que este serviço publica um evento (antes só consumia).
/// </summary>
public record ApoioSolicitadoEvent(Guid EventId, Guid UsuarioId, DateTime OcorreuEm);
