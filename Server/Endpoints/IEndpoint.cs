namespace Server.Endpoints;

public interface IEndpoint
{
    public static abstract void RegisterEndpoints(WebApplication app);
}