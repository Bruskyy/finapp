using FluentValidation;
using Usuarios.Api.Contratos;
using Usuarios.Api.Dominio;

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

public class PerfilOnboardingRequestValidator : AbstractValidator<PerfilOnboardingRequest>
{
    public PerfilOnboardingRequestValidator()
    {
        RuleFor(x => x.MomentoDeVida).IsInEnum();
        RuleFor(x => x.MaiorObjetivo).IsInEnum();
        RuleFor(x => x.MaiorDificuldade).IsInEnum();
        RuleFor(x => x.ValorMensalDesejado).GreaterThan(0);
        RuleFor(x => x.ValorAlvoObjetivo).GreaterThan(0);
        RuleFor(x => x.NomeObjetivoPersonalizado)
            .NotEmpty()
            .WithMessage("Nome do objetivo é obrigatório quando o objetivo é 'Outro'.")
            .When(x => x.MaiorObjetivo == MaiorObjetivo.Outro);
    }
}
