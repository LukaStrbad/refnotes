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
    private readonly string _baseDir;
    private readonly EncryptionService _encryptionService;
    private byte[]? _aesKey;
    private byte[]? _aesIv;

    public Browser(IServiceProvider services)
    {
        var dataDir = Configuration.AppConfig.DataDir;
        ArgumentNullException.ThrowIfNull(dataDir);
        _baseDir = dataDir;
        _encryptionService = services.GetRequiredService<EncryptionService>();
    }

    public void RegisterEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/browser").RequireAuthorization();

        group.MapGet("/list", (string path = "") =>
        {
            var directoryInfo = new DirectoryInfo(Path.Combine(_baseDir, path));
            var files = directoryInfo.GetFiles().Select(file => new UserFile(file.FullName)).ToList();
            var directories = directoryInfo.GetDirectories().Select(directory => directory.Name).ToList();

            var userDirectory = new UserDirectory(path, files, directories);
            return TypedResults.Ok(userDirectory);
        });

        group.MapPost("/uploadText",
            async Task<Results<Ok, UnauthorizedHttpResult>>
            ([FromQuery] string path, HttpContext context, ClaimsPrincipal user) =>
            {
                using var sr = new StreamReader(context.Request.Body);
                var text = await sr.ReadToEndAsync();
                var userDir = user.GetUserDir(_baseDir);
                if (!Directory.Exists(userDir))
                {
                    Directory.CreateDirectory(userDir);
                }
                var fullPath = Path.Combine(userDir, _encryptionService.EncryptAesStringBase64(path));
                var bytes = Encoding.UTF8.GetBytes(text);
                var encrypted = _encryptionService.EncryptAes(bytes);
                await File.WriteAllBytesAsync(fullPath, encrypted);
                return TypedResults.Ok();
            });

        group.MapGet("/downloadText", (string path, ClaimsPrincipal user) =>
        {
            var fullPath = Path.Combine(user.GetUserDir(_baseDir), path);
            var encrypted = File.ReadAllBytes(fullPath);
            var decrypted = _encryptionService.DecryptAes(encrypted).ToArray();
            var text = Encoding.UTF8.GetString(decrypted);
            return TypedResults.Ok(text);
        });
    }
}