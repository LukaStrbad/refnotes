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

    private static async Task HandleException(Exception ex, HttpContext httpContext)
    {
        if (ex is NoNameException noNameException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsync(noNameException.Message);
        }
        else
        {
            Console.Error.WriteLine(ex);
            httpContext.Response.StatusCode = 500;
            await httpContext.Response.WriteAsync("An error occurred");
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