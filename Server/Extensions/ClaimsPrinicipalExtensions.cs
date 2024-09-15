using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Server.Exceptions;

namespace Server.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Returns the directory for the user.
    /// </summary>
    /// <param name="user">this ClaimsPrincipal object</param>
    /// <param name="baseDir">Base directory where files are stored</param>
    /// <returns>A string path to the user directory</returns>
    /// <exception cref="NoNameException">ClaimsPrincipal Identity or Identity.Name is null or empty</exception>
    public static string GetUserDir(this ClaimsPrincipal user, string baseDir)
    {
        if (user.Identity?.Name is not { } name || name.IsNullOrEmpty())
        {
            throw new NoNameException();
        }

        return Path.Combine(baseDir, name);
    }
}