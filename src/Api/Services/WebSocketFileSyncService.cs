using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Api.Model;

namespace Api.Services;

public sealed class WebSocketFileSyncService : IWebSocketFileSyncService
{
    private readonly IFileSyncService _fileSyncService;
    private readonly IWebSocketMessageHandler _webSocketMessageHandler;
    private readonly ILogger<WebSocketFileSyncService> _logger;

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public WebSocketFileSyncService(IFileSyncService fileSyncService, ILogger<WebSocketFileSyncService> logger,
        IWebSocketMessageHandler webSocketMessageHandler)
    {
        _fileSyncService = fileSyncService;
        _logger = logger;
        _webSocketMessageHandler = webSocketMessageHandler;
    }

    public async Task HandleFileSync(WebSocket webSocket, int fileId, CancellationToken cancellationToken)
    {
        string? clientId = null;
        await _fileSyncService.SubscribeToSyncSignalAsync(fileId, async channelMessage =>
        {
            // Don't send the message to the client that updated the file
            if (clientId == channelMessage.ClientId) return;

            var message = new FileUpdatedMessage(channelMessage.UpdatedAt, channelMessage.ClientId);
            await _webSocketMessageHandler.WriteMessage(webSocket, message, JsonOptions, cancellationToken);
        }, cancellationToken);

        while (webSocket.State == WebSocketState.Open)
        {
            var readResult = await _webSocketMessageHandler.ReadMessage(webSocket, cancellationToken);
            if (readResult.Closed)
                break;

            _logger.LogInformation("Received message: {Message}", readResult.Message);

            if (JsonSerializer.Deserialize<ClientIdMessage>(readResult.Message, JsonOptions) is { } clientIdMessage)
            {
                // ReSharper disable once RedundantAssignment
                clientId = clientIdMessage.ClientId;
            }
        }

        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Sync finished", cancellationToken);
    }
}
