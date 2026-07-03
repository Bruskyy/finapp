using FluentValidation.TestHelper;
using Lancamentos.Api.Contratos;
using Lancamentos.Api.Validacao;
using Lancamentos.Domain.Entidades;

namespace Lancamentos.Tests.Validacao;

public class CriarLancamentoRequestValidatorTests
{
    private readonly CriarLancamentoRequestValidator _validator = new();

    private static CriarLancamentoRequest RequestValido() =>
        new("Almoço", 35.50m, TipoLancamento.Despesa, Guid.NewGuid(), DateTime.Today);

    [Fact]
    public void RequestValido_NaoDeveTerErros()
    {
        var resultado = _validator.TestValidate(RequestValido());

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void DescricaoVazia_DeveFalhar()
    {
        var req = RequestValido() with { Descricao = "" };

        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Descricao);
    }

    [Fact]
    public void ValorZero_DeveFalhar()
    {
        var req = RequestValido() with { Valor = 0 };

        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Valor);
    }

    [Fact]
    public void TipoForaDoEnum_DeveFalhar()
    {
        var req = RequestValido() with { Tipo = (TipoLancamento)99 };

        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Tipo);
    }

    [Fact]
    public void CategoriaVazia_DeveFalhar()
    {
        var req = RequestValido() with { CategoriaId = Guid.Empty };

        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.CategoriaId);
    }
}

public class DefinirOrcamentoRequestValidatorTests
{
    private readonly DefinirOrcamentoRequestValidator _validator = new();

    [Fact]
    public void RequestValido_NaoDeveTerErros()
    {
        var resultado = _validator.TestValidate(new DefinirOrcamentoRequest(Guid.NewGuid(), 500m));

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void LimiteInvalido_DeveFalhar(decimal limite)
    {
        var resultado = _validator.TestValidate(new DefinirOrcamentoRequest(Guid.NewGuid(), limite));

        resultado.ShouldHaveValidationErrorFor(x => x.ValorLimite);
    }
}
