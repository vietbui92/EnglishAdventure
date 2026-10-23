using EnglishAdventure.Application.Common.Exceptions;
using EnglishAdventure.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EnglishAdventure.Api.ExceptionHandling;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail, errors) = MapException(exception);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning(exception, "Request failed with {StatusCode} for {Method} {Path}", statusCode, httpContext.Request.Method, httpContext.Request.Path);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = _environment.IsDevelopment() ? detail : GetSafeDetail(statusCode, detail),
            Instance = httpContext.Request.Path
        };

        if (errors is not null)
        {
            problemDetails.Extensions["errors"] = errors;
        }

        if (exception is AuthenticationForbiddenException { Code: { } code })
        {
            problemDetails.Extensions["code"] = code;
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Title, string Detail, object? Errors) MapException(Exception exception)
    {
        return exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                validationException.Message,
                validationException.Errors
                    .GroupBy(error => error.PropertyName)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(error => error.ErrorMessage).ToArray())),

            NotFoundException notFoundException => (
                StatusCodes.Status404NotFound,
                "Resource not found.",
                notFoundException.Message,
                null),

            AuthenticationFailedException or RefreshTokenException or UnauthorizedException => (
                StatusCodes.Status401Unauthorized,
                "Authentication failed.",
                exception.Message,
                null),

            ConflictException conflictException => (
                StatusCodes.Status409Conflict,
                "Conflict.",
                conflictException.Message,
                null),

            AuthenticationForbiddenException => (
                StatusCodes.Status403Forbidden,
                "Access denied.",
                exception.Message,
                null),

            DomainException domainException => (
                StatusCodes.Status422UnprocessableEntity,
                "Domain rule violation.",
                domainException.Message,
                null),

            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                exception.Message,
                null)
        };
    }

    private static string GetSafeDetail(int statusCode, string detail)
    {
        return statusCode >= StatusCodes.Status500InternalServerError
            ? "An unexpected error occurred. Contact support if the problem persists."
            : detail;
    }
}
