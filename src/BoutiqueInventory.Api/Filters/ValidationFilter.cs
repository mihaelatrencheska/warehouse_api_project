using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace BoutiqueInventory.Api.Filters;

/// <summary>
/// MVC action filter that runs every registered FluentValidation
/// validator against each action argument. Failures short-circuit the
/// pipeline with an RFC 7807 <c>ValidationProblemDetails</c> response.
/// </summary>
public sealed class ValidationFilter(
    IServiceProvider services,
    ProblemDetailsFactory problemDetailsFactory) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, argument) in context.ActionArguments)
        {
            if (argument is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (services.GetService(validatorType) is not IValidator validator) continue;

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);
            if (result.IsValid) continue;

            foreach (var failure in result.Errors)
            {
                var member = string.IsNullOrEmpty(failure.PropertyName) ? key : failure.PropertyName;
                if (!errors.TryGetValue(member, out var bucket))
                {
                    bucket = new List<string>();
                    errors[member] = bucket;
                }
                bucket.Add(failure.ErrorMessage);
            }
        }

        if (errors.Count == 0)
        {
            await next();
            return;
        }

        var modelStateErrors = errors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
        var problem = problemDetailsFactory.CreateValidationProblemDetails(
            context.HttpContext,
            new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary());

        problem.Status = StatusCodes.Status400BadRequest;
        problem.Title = "One or more validation errors occurred.";
        problem.Errors.Clear();
        foreach (var (key, value) in modelStateErrors)
        {
            problem.Errors[key] = value;
        }

        context.Result = new BadRequestObjectResult(problem)
        {
            ContentTypes = { "application/problem+json" }
        };
    }
}
