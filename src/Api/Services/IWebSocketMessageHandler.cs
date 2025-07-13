using System.Net.WebSockets;
using System.Text.Json;
using Api.Model;

namespace Api.Services;

public interface IWebSocketMessageHandler
{
    Task<ReadMessageResult> ReadMessage(WebSocket webSocket, CancellationToken cancellationToken);
    Task WriteMessage(WebSocket webSocket, string message, CancellationToken cancellationToken);

    Task WriteMessage<T>(WebSocket webSocket, T message, JsonSerializerOptions jsonOptions,
        CancellationToken cancellationToken)
        where T : FileSyncMessage;
}
