using Google.Apis.Auth;

namespace Usuarios.Api.Aplicacao;

/// <summary>
/// Isola a chamada estática do Google.Apis.Auth atrás de uma interface -
/// sem isso, não dá pra testar o fluxo de login Google sem um token real
/// assinado pelo Google (o AuthEndpointsTests usa uma implementação fake
/// que devolve um payload conhecido, sem bater na rede).
/// </summary>
public interface IGoogleIdTokenValidator
{
    Task<GoogleJsonWebSignature.Payload> ValidarAsync(string idToken, string clientId);
}

public class GoogleIdTokenValidator : IGoogleIdTokenValidator
{
    public Task<GoogleJsonWebSignature.Payload> ValidarAsync(string idToken, string clientId) =>
        GoogleJsonWebSignature.ValidateAsync(
            idToken,
            new GoogleJsonWebSignature.ValidationSettings { Audience = [clientId] });
}
