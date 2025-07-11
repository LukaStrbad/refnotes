using Api.Exceptions;

namespace Api.Middlewares;

public class ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
{

    public async Task Invoke(HttpContext httpContext)
    {
        try
        {
            await next(httpContext);
        }
        catch (Exception e)
        {
            await HandleException(e, httpContext);
        }
    }

    private async Task HandleException(Exception e, HttpContext httpContext)
    {
        if (httpContext.WebSockets.IsWebSocketRequest)
        {
            logger.LogError(e, "WebSocket exception");
            return;
        }
        
        switch (e)
        {
            case NoNameException noNameException:
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsync(noNameException.Message);
                logger.LogError(e, "No name exception");
                break;
            case DirectoryAlreadyExistsException or FileAlreadyExistsException:
                httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
                await httpContext.Response.WriteAsync(e.Message);
                logger.LogWarning(e, "Directory or file already exists");
                break;
            case DirectoryNotFoundException or FileNotFoundException:
                httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                await httpContext.Response.WriteAsync(e.Message);
                logger.LogWarning(e, "Directory or file not found");
                break;
            case ForbiddenException:
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                await httpContext.Response.WriteAsync(e.Message);
                logger.LogWarning(e, "Forbidden");
                break;
            default:
                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await httpContext.Response.WriteAsync("An error occurred");
                logger.LogError(e, "An error occurred");
                break;
        }
    }
}

public static class ExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandlerMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlerMiddleware>();
    }
}
