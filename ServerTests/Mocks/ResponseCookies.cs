using Microsoft.AspNetCore.Http;

namespace ServerTests.Mocks;

public class ResponseCookies: IResponseCookies
{
    public Dictionary<string, string> Cookies { get; } = new();
    public Dictionary<string, CookieOptions> Options { get; } = new();
    
    public void Append(string key, string value)
    {
        Cookies[key] = value;
    }

    public void Append(string key, string value, CookieOptions options)
    {
        Cookies[key] = value;
        Options[key] = options;
    }

    public void Delete(string key)
    {
        throw new NotImplementedException();
    }

    public void Delete(string key, CookieOptions options)
    {
        throw new NotImplementedException();
    }
}