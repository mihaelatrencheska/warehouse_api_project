using BoutiqueInventory.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace BoutiqueInventory.Api.Middleware;

/// <summary>
/// Translates domain exceptions and unhandled errors into RFC 7807
/// <see cref="ProblemDetails"/> responses with appropriate HTTP status codes.
/// </summary>
public sealed class GlobalExceptionHandler(
    ProblemDetailsFactory problemDetailsFactory,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ProblemDetails problem;

        switch (exception)
        {
            case NotFoundException notFound:
                problem = problemDetailsFactory.CreateProblemDetails(
                    httpContext,
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Resource not found",
                    detail: notFound.Message);
                break;

            case ConflictException conflict:
                problem = problemDetailsFactory.CreateProblemDetails(
                    httpContext,
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Conflict",
                    detail: conflict.Message);
                break;

            case DomainValidationException validation:
                var validationProblem = problemDetailsFactory.CreateValidationProblemDetails(
                    httpContext,
                    new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary());
                validationProblem.Status = StatusCodes.Status400BadRequest;
                validationProblem.Title = "Validation failed";
                validationProblem.Errors.Clear();
                foreach (var (key, value) in validation.Errors)
                {
                    validationProblem.Errors[key] = value;
                }
                problem = validationProblem;
                break;

            default:
                logger.LogError(exception, "Unhandled exception while processing {Path}", httpContext.Request.Path);
                problem = problemDetailsFactory.CreateProblemDetails(
                    httpContext,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "An unexpected error occurred.");
                break;
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken: cancellationToken);
        return true;
    }
}
