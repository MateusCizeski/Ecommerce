using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace Api.Middleware.Exceptions;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions _json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception ex) { await HandleAsync(context, ex); }
    }

    private async Task HandleAsync(HttpContext ctx, Exception ex)
    {
        var (status, title, detail, errors) = ex switch
        {
            Application.Common.Exceptions.ValidationException ve =>
                ((int)HttpStatusCode.UnprocessableEntity, "Validation failed", ve.Message, (object?)ve.Errors),
            NotFoundException nfe => (404, "Resource not found", nfe.Message, null),
            TenantAccessException tae => (403, "Access denied", tae.Message, null),
            ForbiddenException fe => (403, "Forbidden", fe.Message, null),
            ConflictException ce => (409, "Conflict", ce.Message, null),
            ConcurrencyException cce => (409, "Concurrency conflict", cce.Message, null),
            DbUpdateConcurrencyException => (409, "Concurrency conflict", "The resource was modified by another operation. Please refresh and try again.", null),
            DomainException de => (400, "Domain error", de.Message, null),
            PaymentException pe => (402, "Payment failed", pe.Message, null),
            _ => (500, "An unexpected error occurred", "Please try again later.", null)
        };

        if (status == 500) logger.LogError(ex, "Unhandled exception");
        else logger.LogWarning(ex, "Handled exception [{Status}]", status);

        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail, Instance = ctx.Request.Path };
        if (errors is not null) problem.Extensions["errors"] = errors;

        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/problem+json";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(problem, _json));
    }
}
