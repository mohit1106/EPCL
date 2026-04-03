using Microsoft.Extensions.Logging;
using IdentityService.Domain.Exceptions;
using FluentValidation;

namespace IdentityService.API.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                statusCode = 400,
                message = "Validation failed",
                errors = ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }),
                correlationId = context.Request.Headers["X-Correlation-ID"].ToString(),
                timestamp = DateTimeOffset.UtcNow
            });
        }
        catch (AccountLockedException ex)
        {
            context.Response.StatusCode = 423;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                statusCode = 423,
                message = ex.Message,
                lockoutEnd = ex.LockoutEnd,
                correlationId = context.Request.Headers["X-Correlation-ID"].ToString(),
                timestamp = DateTimeOffset.UtcNow
            });
        }
        catch (InvalidCredentialsException ex)
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                statusCode = 401,
                message = ex.Message,
                correlationId = context.Request.Headers["X-Correlation-ID"].ToString(),
                timestamp = DateTimeOffset.UtcNow
            });
        }
        catch (DuplicateEntityException ex)
        {
            context.Response.StatusCode = 409;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                statusCode = 409,
                message = ex.Message,
                correlationId = context.Request.Headers["X-Correlation-ID"].ToString(),
                timestamp = DateTimeOffset.UtcNow
            });
        }
        catch (NotFoundException ex)
        {
            context.Response.StatusCode = 404;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                statusCode = 404,
                message = ex.Message,
                correlationId = context.Request.Headers["X-Correlation-ID"].ToString(),
                timestamp = DateTimeOffset.UtcNow
            });
        }
        catch (DomainException ex)
        {
            context.Response.StatusCode = 422;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                statusCode = 422,
                message = ex.Message,
                correlationId = context.Request.Headers["X-Correlation-ID"].ToString(),
                timestamp = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            var corrId = context.Request.Headers["X-Correlation-ID"].ToString();
            logger.LogError(ex, "Unhandled exception. CorrelationId: {CorrelationId}", corrId);
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                statusCode = 500,
                message = "An internal server error occurred.",
                correlationId = corrId,
                timestamp = DateTimeOffset.UtcNow
            });
        }
    }
}
