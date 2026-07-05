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
