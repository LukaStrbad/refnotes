using Server.Exceptions;

namespace Server.Middlewares;

public class ExceptionHandlerMiddleware(RequestDelegate next)
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

    private static async Task HandleException(Exception e, HttpContext httpContext)
    {
        switch (e)
        {
            case NoNameException noNameException:
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await httpContext.Response.WriteAsync(noNameException.Message);
                break;
            case DirectoryAlreadyExistsException or FileAlreadyExistsException:
                httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
                await httpContext.Response.WriteAsync(e.Message);
                break;
            case DirectoryNotFoundException or FileNotFoundException:
                httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                await httpContext.Response.WriteAsync(e.Message);
                break;
            default:
                Console.Error.WriteLine(e);
                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await httpContext.Response.WriteAsync("An error occurred");
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