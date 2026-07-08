using FluentValidation;
using Usuarios.Api.Contratos;

namespace Usuarios.Api.Validacao;

public class RegistrarRequestValidator : AbstractValidator<RegistrarRequest>
{
    public RegistrarRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Senha).NotEmpty().MinimumLength(8);
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Senha).NotEmpty();
    }
}

public class AtualizarPerfilRequestValidator : AbstractValidator<AtualizarPerfilRequest>
{
    public AtualizarPerfilRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(100);
    }
}

public class TrocarSenhaRequestValidator : AbstractValidator<TrocarSenhaRequest>
{
    public TrocarSenhaRequestValidator()
    {
        RuleFor(x => x.SenhaAtual).NotEmpty();
        RuleFor(x => x.NovaSenha).NotEmpty().MinimumLength(8);
    }
}

public class LoginGoogleRequestValidator : AbstractValidator<LoginGoogleRequest>
{
    public LoginGoogleRequestValidator()
    {
        RuleFor(x => x.IdToken).NotEmpty();
    }
}

public class RenovarTokenRequestValidator : AbstractValidator<RenovarTokenRequest>
{
    public RenovarTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
