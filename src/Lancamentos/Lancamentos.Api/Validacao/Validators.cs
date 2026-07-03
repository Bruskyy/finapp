using FluentValidation;
using Lancamentos.Api.Contratos;

namespace Lancamentos.Api.Validacao;

public class CriarLancamentoRequestValidator : AbstractValidator<CriarLancamentoRequest>
{
    public CriarLancamentoRequestValidator()
    {
        RuleFor(x => x.Descricao).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Valor).GreaterThan(0);
        RuleFor(x => x.Tipo).IsInEnum();
        RuleFor(x => x.CategoriaId).NotEmpty();
        RuleFor(x => x.Data).NotEmpty();
    }
}

public class AtualizarLancamentoRequestValidator : AbstractValidator<AtualizarLancamentoRequest>
{
    public AtualizarLancamentoRequestValidator()
    {
        RuleFor(x => x.Descricao).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Valor).GreaterThan(0);
        RuleFor(x => x.Tipo).IsInEnum();
        RuleFor(x => x.CategoriaId).NotEmpty();
        RuleFor(x => x.Data).NotEmpty();
    }
}

public class CriarCategoriaRequestValidator : AbstractValidator<CriarCategoriaRequest>
{
    public CriarCategoriaRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(100);
    }
}

public class DefinirOrcamentoRequestValidator : AbstractValidator<DefinirOrcamentoRequest>
{
    public DefinirOrcamentoRequestValidator()
    {
        RuleFor(x => x.CategoriaId).NotEmpty();
        RuleFor(x => x.ValorLimite).GreaterThan(0);
    }
}
