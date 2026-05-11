using System.Net;
using System.Text.Json;
using TripPlanner.Application.Common.Exceptions;

namespace TripPlanner.API.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(ctx, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext ctx, Exception ex)
    {
        var (status, message, errors) = ex switch
        {
            NotFoundException e => (HttpStatusCode.NotFound, e.Message, (IEnumerable<string>?)[]),
            UnauthorizedException e => (HttpStatusCode.Unauthorized, e.Message, (IEnumerable<string>?)[]),
            Application.Common.Exceptions.ValidationException e => (HttpStatusCode.BadRequest, e.Message, e.Errors),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", (IEnumerable<string>?)[])
        };

        ctx.Response.ContentType = "application/json";
        ctx.Response.StatusCode = (int)status;

        var body = JsonSerializer.Serialize(new { message, errors });
        return ctx.Response.WriteAsync(body);
    }
}
