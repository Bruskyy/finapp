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
        RuleFor(x => x.Tipo).IsInEnum();

        // Validação condicional por tipo (clássico de entrevista com Fluent
        // Validation): os campos de cartão são obrigatórios SÓ quando o tipo
        // é cartão - o domínio (Conta.CriarCartao) revalida as invariantes,
        // duas camadas de defesa como no resto do projeto.
        When(x => x.Tipo == Lancamentos.Domain.Entidades.TipoConta.Cartao, () =>
        {
            RuleFor(x => x.Limite).NotNull().GreaterThan(0);
            RuleFor(x => x.DiaFechamento).NotNull().InclusiveBetween(1, 28);
            RuleFor(x => x.DiaVencimento).NotNull().InclusiveBetween(1, 28)
                .NotEqual(x => x.DiaFechamento).WithMessage("Fechamento e vencimento não podem ser no mesmo dia.");
        });
    }
}

public class CriarCompraParceladaRequestValidator : AbstractValidator<CriarCompraParceladaRequest>
{
    public CriarCompraParceladaRequestValidator()
    {
        RuleFor(x => x.Descricao).NotEmpty().MaximumLength(180); // sobra espaço pro sufixo " (NN/NN)" dentro dos 200 da coluna
        RuleFor(x => x.ValorTotal).GreaterThan(0);
        RuleFor(x => x.NumeroParcelas).InclusiveBetween(2, 48);
        RuleFor(x => x.CategoriaId).NotEmpty();
        RuleFor(x => x.ContaId).NotEmpty();
        RuleFor(x => x.Data).NotEmpty();

        // Valor por parcela derivado (ValorTotal/NumeroParcelas, arredondado
        // pra baixo em CompraParcelada.cs) precisa ser >= 1 centavo - sem
        // isso, um valor pequeno dividido em muitas parcelas gera parcela de
        // R$0,00, que Lancamento.Validar rejeita com ArgumentException. Não
        // há middleware de exceção global no projeto, então isso virava 500
        // cru em vez de 400 amigável.
        RuleFor(x => x)
            .Must(x => x.ValorTotal / x.NumeroParcelas >= 0.01m)
            .WithName("ValorTotal")
            .WithMessage("O valor de cada parcela ficaria abaixo de R$ 0,01 - reduza o número de parcelas ou aumente o valor.")
            .When(x => x.NumeroParcelas > 0);
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
