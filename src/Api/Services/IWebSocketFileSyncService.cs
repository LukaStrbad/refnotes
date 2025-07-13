using System.Net.WebSockets;

namespace Api.Services;

public interface IWebSocketFileSyncService
{
    Task HandleFileSync(WebSocket webSocket, int fileId, CancellationToken cancellationToken);
}
