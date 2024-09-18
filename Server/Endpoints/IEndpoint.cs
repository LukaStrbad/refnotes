namespace Server.Endpoints;

public interface IEndpoint
{
    public void RegisterEndpoints(WebApplication app);
}