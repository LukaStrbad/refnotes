using Api.Model;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Base;

public class AuthControllerBase : ControllerBase
{
    private readonly IAppDomainService _appDomainService;
    private readonly AppSettings _appSettings;

    public AuthControllerBase(IAppDomainService appDomainService, AppSettings appSettings)
    {
        _appDomainService = appDomainService;
        _appSettings = appSettings;
    }

    private CookieOptions GetCookieOptions(bool httpOnly, DateTimeOffset? expires = null)
    {
        // Check if the request is from a valid domain, if not, set the cookie domain to localhost
        var requestDomain = HttpContext.Request.Host.Host;
        var cookieDomain = _appDomainService.IsAppDomain(requestDomain) ? requestDomain : "localhost";

        var options = new CookieOptions
        {
            Domain = cookieDomain,
            SameSite = SameSiteMode.Strict,
            IsEssential = true,
            HttpOnly = httpOnly,
            Secure = _appSettings.CookieSecure
        };

        if (expires is not null)
        {
            options.Expires = expires;
        }

        return options;
    }

    protected void AddTokenCookies(Tokens tokens)
    {
        // Set an access token cookie (set the same expiration time as the refresh token)
        HttpContext.Response.Cookies.Append("accessToken", tokens.AccessToken,
            GetCookieOptions(false, tokens.RefreshToken.ExpiryTime));
        // Set refresh token cookie   
        HttpContext.Response.Cookies.Append("refreshToken", tokens.RefreshToken.Token,
            GetCookieOptions(true, tokens.RefreshToken.ExpiryTime));
    }
}
