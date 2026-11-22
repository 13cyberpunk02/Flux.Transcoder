using FluentValidation;
using Flux.Transcoder.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flux.Transcoder.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)   // FluentValidation
        {
            await WriteProblem(context, StatusCodes.Status400BadRequest,
                "Validation failed",
                string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)));
        }
        catch (NotFoundException ex)
        {
            await WriteProblem(context, StatusCodes.Status404NotFound, "Not found", ex.Message);
        }
        catch (InvalidStateTransitionException ex)
        {
            await WriteProblem(context, StatusCodes.Status409Conflict, "Invalid state transition", ex.Message);
        }
        catch (DbUpdateConcurrencyException)   // повторы исчерпаны: канал меняют одновременно
        {
            await WriteProblem(context, StatusCodes.Status409Conflict, "Concurrent update",
                "Канал одновременно изменён другим действием, повторите.");
        }
        catch (UnauthorizedException ex)
        {
            await WriteProblem(context, StatusCodes.Status401Unauthorized, "Unauthorized", ex.Message);
        }
        catch (DomainException ex)       // прочие доменные нарушения
        {
            await WriteProblem(context, StatusCodes.Status400BadRequest, "Domain rule violated", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteProblem(context, StatusCodes.Status500InternalServerError,
                "Internal server error", "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblem(
        HttpContext context, int statusCode, string title, string detail)
    {
        if (context.Response.HasStarted) return;

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        });
    }
}

public static class ExceptionHandlingExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionHandlingMiddleware>();
}