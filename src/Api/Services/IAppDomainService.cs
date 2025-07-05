namespace Api.Services;

public interface IAppDomainService
{
    /// <summary>
    /// Checks if the current domain is in the app domain list.
    /// </summary>
    /// <param name="domain">Domain to check</param>
    /// <returns>True if the domain is in the app domain list, false otherwise</returns>
    bool IsAppDomain(string domain);

    /// <summary>
    /// Extracts the domain from the URL and checks if it is in the app domain list.
    /// </summary>
    /// <param name="url">URL to check</param>
    /// <returns>True if the domain is in the app domain list, false otherwise</returns>
    bool IsUrlAppDomain(string url);
}
