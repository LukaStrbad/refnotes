using Server.Model;

namespace Server.Endpoints;

public class Browser : IEndpoint
{
    public static void RegisterEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/browser").RequireAuthorization();
        group.MapGet("/list", (HttpContext context, string path = "./") =>
        {
            Console.WriteLine($"{context.Connection.RemoteIpAddress}:{context.Connection.RemotePort}");
            var directoryInfo = new DirectoryInfo(path);
            var files = directoryInfo.GetFiles().Select(file => new UserFile(file.FullName)).ToList();
            var directories = directoryInfo.GetDirectories().Select(directory => directory.FullName).ToList();

            var userDirectory = new UserDirectory(path, files, directories);
            return TypedResults.Ok(userDirectory);
        });
    }
}