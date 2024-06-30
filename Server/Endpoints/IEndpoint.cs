namespace Server.Endpoints;

public interface IEndpoint
{
    public static abstract void RegisterEndpoints(IEndpointRouteBuilder routes);
}