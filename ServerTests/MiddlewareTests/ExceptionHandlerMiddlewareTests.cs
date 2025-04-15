using Microsoft.AspNetCore.Http;
using Server.Exceptions;
using Server.Middlewares;

namespace ServerTests.MiddlewareTests;

public class ExceptionHandlerMiddlewareTests : BaseTests
{
    private readonly HttpContext _httpContext;
    private readonly MemoryStream _stream;

    public ExceptionHandlerMiddlewareTests()
    {
        _stream = new MemoryStream();
        _httpContext = new DefaultHttpContext
        {
            Response = { Body = _stream }
        };
    }

    private async Task<string> ReadStream()
    {
        _stream.Position = 0;
        using var reader = new StreamReader(_stream);
        return await reader.ReadToEndAsync();
    }

    [Fact]
    public async Task Invoke_SetsBadRequest_WhenNoNameExceptionIsThrown()
    {
        var middleware = new ExceptionHandlerMiddleware(Next);

        await middleware.Invoke(_httpContext);

        Assert.Equal(StatusCodes.Status400BadRequest, _httpContext.Response.StatusCode);
        Assert.NotEmpty(await ReadStream());
        return;

        static Task Next(HttpContext _) => throw new NoNameException();
    }

    [Fact]
    public async Task Invoke_SetsConflict_WhenDirectoryAlreadyExistsExceptionIsThrown()
    {
        const string message = "Exception";

        var middleware = new ExceptionHandlerMiddleware(Next);

        await middleware.Invoke(_httpContext);

        Assert.Equal(StatusCodes.Status409Conflict, _httpContext.Response.StatusCode);
        Assert.Equal(message, await ReadStream());
        return;

        Task Next(HttpContext _) => throw new DirectoryAlreadyExistsException(message);
    }

    [Fact]
    public async Task Invoke_SetsConflict_WhenFileAlreadyExistsExceptionIsThrown()
    {
        const string message = "Exception";
        
        var middleware = new ExceptionHandlerMiddleware(Next);

        await middleware.Invoke(_httpContext);

        Assert.Equal(StatusCodes.Status409Conflict, _httpContext.Response.StatusCode);
        Assert.Equal(message, await ReadStream());
        return;

        Task Next(HttpContext _) => throw new FileAlreadyExistsException(message);
    }
    
    [Fact]
    public async Task Invoke_SetsNotFound_WhenDirectoryNotFoundExceptionIsThrown()
    {
        const string message = "Exception";

        var middleware = new ExceptionHandlerMiddleware(Next);

        await middleware.Invoke(_httpContext);

        Assert.Equal(StatusCodes.Status404NotFound, _httpContext.Response.StatusCode);
        Assert.Equal(message, await ReadStream());
        return;

        Task Next(HttpContext _) => throw new DirectoryNotFoundException(message);
    }
    
    [Fact]
    public async Task Invoke_SetsNotFound_WhenFileNotFoundExceptionIsThrown()
    {
        const string message = "Exception";

        var middleware = new ExceptionHandlerMiddleware(Next);

        await middleware.Invoke(_httpContext);

        Assert.Equal(StatusCodes.Status404NotFound, _httpContext.Response.StatusCode);
        Assert.Equal(message, await ReadStream());
        return;

        Task Next(HttpContext _) => throw new FileNotFoundException(message);
    }
    
    [Fact]
    public async Task Invoke_SetsInternalServerError_WhenOtherExceptionIsThrown()
    {
        var middleware = new ExceptionHandlerMiddleware(Next);

        await middleware.Invoke(_httpContext);

        Assert.Equal(StatusCodes.Status500InternalServerError, _httpContext.Response.StatusCode);
        Assert.Equal("An error occurred", await ReadStream());
        return;

        Task Next(HttpContext _) => throw new Exception("Exception");
    }
}