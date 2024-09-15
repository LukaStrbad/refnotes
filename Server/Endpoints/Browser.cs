using System.Security.Claims;
using Server.Model;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Server.Extensions;
using Server.Services;

namespace Server.Endpoints;

public class Browser : IEndpoint
{
    private static readonly string BaseDir = Configuration.AppConfig.DataDir!;
    private static byte[]? _aesKey;
    private static byte[]? _aesIv;

    public static void RegisterEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/browser").RequireAuthorization();

        group.MapGet("/list", (string path = "") =>
        {
            var directoryInfo = new DirectoryInfo(Path.Combine(BaseDir, path));
            var files = directoryInfo.GetFiles().Select(file => new UserFile(file.FullName)).ToList();
            var directories = directoryInfo.GetDirectories().Select(directory => directory.Name).ToList();

            var userDirectory = new UserDirectory(path, files, directories);
            return TypedResults.Ok(userDirectory);
        });

        group.MapPost("/uploadText",
            async Task<Results<Ok, UnauthorizedHttpResult>>
            ([FromQuery] string path, HttpContext context, ClaimsPrincipal user,
                EncryptionService encryptionService) =>
            {
                using var sr = new StreamReader(context.Request.Body);
                var text = await sr.ReadToEndAsync();
                var userDir = user.GetUserDir(BaseDir);
                if (!Directory.Exists(userDir))
                {
                    Directory.CreateDirectory(userDir);
                }
                var fullPath = Path.Combine(userDir, encryptionService.EncryptAesStringBase64(path));
                var bytes = Encoding.UTF8.GetBytes(text);
                var encrypted = encryptionService.EncryptAes(bytes);
                await File.WriteAllBytesAsync(fullPath, encrypted);
                return TypedResults.Ok();
            });

        group.MapGet("/downloadText", (string path, EncryptionService encryptionService, ClaimsPrincipal user) =>
        {
            var fullPath = Path.Combine(user.GetUserDir(BaseDir), path);
            var encrypted = File.ReadAllBytes(fullPath);
            var decrypted = encryptionService.DecryptAes(encrypted).ToArray();
            var text = Encoding.UTF8.GetString(decrypted);
            return TypedResults.Ok(text);
        });
    }
}