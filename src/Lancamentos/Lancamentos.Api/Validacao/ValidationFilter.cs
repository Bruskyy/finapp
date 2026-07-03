using FluentValidation;

namespace Lancamentos.Api.Validacao;

/// <summary>
/// Endpoint filter que roda o validator do Fluent Validation antes do handler.
/// Se o DTO for inválido, devolve 400 com o detalhe dos erros (ProblemDetails)
/// sem nem chegar na lógica do endpoint.
/// </summary>
public class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is not null)
        {
            var argumento = context.Arguments.OfType<T>().FirstOrDefault();
            if (argumento is not null)
            {
                var resultado = await validator.ValidateAsync(argumento, context.HttpContext.RequestAborted);
                if (!resultado.IsValid)
                    return Results.ValidationProblem(resultado.ToDictionary());
            }
        }

        return await next(context);
    }
}
