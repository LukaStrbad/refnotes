using System.Security.Claims;
using Server.Model;

namespace Server.Db;

public interface IBrowserServiceRepository
{
    Task<ResponseDirectory?> List(ClaimsPrincipal claimsPrincipal, string path = "/");
    Task<string> AddFile(ClaimsPrincipal claimsPrincipal, string directoryPath, string name);
    Task<string?> GetFilesystemFilePath(ClaimsPrincipal claimsPrincipal, string directoryPath, string name);
}