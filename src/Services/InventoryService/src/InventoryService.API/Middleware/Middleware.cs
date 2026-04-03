using System.Net;
using System.Text.Json;
using InventoryService.Domain.Exceptions;

namespace InventoryService.API.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext ctx, Exception ex)
    {
        var correlationId = ctx.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? ctx.TraceIdentifier;
        logger.LogError(ex, "Unhandled exception [{CorrelationId}]: {Message}", correlationId, ex.Message);

        var (status, message) = ex switch
        {
            NotFoundException => (HttpStatusCode.NotFound, ex.Message),
            DuplicateEntityException => (HttpStatusCode.Conflict, ex.Message),
            InsufficientCapacityException => (HttpStatusCode.BadRequest, ex.Message),
            InsufficientStockException => (HttpStatusCode.BadRequest, ex.Message),
            DomainException => (HttpStatusCode.BadRequest, ex.Message),
            FluentValidation.ValidationException vex => (HttpStatusCode.BadRequest,
                string.Join("; ", vex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"))),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        ctx.Response.StatusCode = (int)status;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            statusCode = (int)status,
            message,
            correlationId,
            timestamp = DateTimeOffset.UtcNow
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}

public class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        const string header = "X-Correlation-ID";
        if (!context.Request.Headers.ContainsKey(header))
            context.Request.Headers[header] = Guid.NewGuid().ToString();
        context.Response.Headers[header] = context.Request.Headers[header];
        await next(context);
    }
}
