namespace Usuarios.Api.Contratos;

public record RegistrarRequest(string Nome, string Email, string Senha);

public record LoginRequest(string Email, string Senha);

public record TokenResponse(string Token, string Nome, string Email);

public record UsuarioResponse(Guid Id, string Nome, string Email, DateTime CriadoEm);

public record AtualizarPerfilRequest(string Nome);

public record TrocarSenhaRequest(string SenhaAtual, string NovaSenha);
