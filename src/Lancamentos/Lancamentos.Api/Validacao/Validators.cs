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
        RuleFor(x => x.ContaId).NotEmpty();
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
        RuleFor(x => x.ContaId).NotEmpty();
        RuleFor(x => x.Data).NotEmpty();
    }
}

public class CriarContaRequestValidator : AbstractValidator<CriarContaRequest>
{
    public CriarContaRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(100);
    }
}

public class CriarRecorrenciaRequestValidator : AbstractValidator<CriarRecorrenciaRequest>
{
    public CriarRecorrenciaRequestValidator()
    {
        RuleFor(x => x.Descricao).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Valor).GreaterThan(0);
        RuleFor(x => x.Tipo).IsInEnum();
        RuleFor(x => x.CategoriaId).NotEmpty();
        RuleFor(x => x.ContaId).NotEmpty();
        RuleFor(x => x.DiaDoMes).InclusiveBetween(1, 31);
    }
}

public class TransferenciaRequestValidator : AbstractValidator<TransferenciaRequest>
{
    public TransferenciaRequestValidator()
    {
        RuleFor(x => x.ContaOrigemId).NotEmpty();
        RuleFor(x => x.ContaDestinoId)
            .NotEmpty()
            .NotEqual(x => x.ContaOrigemId).WithMessage("Conta de destino deve ser diferente da conta de origem.");
        RuleFor(x => x.Valor).GreaterThan(0);
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

public class CriarObjetivoRequestValidator : AbstractValidator<CriarObjetivoRequest>
{
    public CriarObjetivoRequestValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ValorAlvo).GreaterThan(0);
        RuleFor(x => x.DataAlvo).GreaterThan(_ => DateTime.Today)
            .WithMessage("Data alvo deve ser no futuro.");
    }
}

public class AporteRequestValidator : AbstractValidator<AporteRequest>
{
    public AporteRequestValidator()
    {
        RuleFor(x => x.Valor).GreaterThan(0);
        RuleFor(x => x.ContaId).NotEmpty();
    }
}
