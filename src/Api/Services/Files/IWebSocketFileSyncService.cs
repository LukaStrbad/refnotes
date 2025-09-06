using System.Net.WebSockets;

namespace Api.Services.Files;

public interface IWebSocketFileSyncService
{
    Task HandleFileSync(WebSocket webSocket, int fileId, CancellationToken cancellationToken);
}
