using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Api.Model;

namespace Api.Services;

public sealed class WebSocketFileSyncService : IWebSocketFileSyncService, IDisposable
{
    private readonly IFileSyncService _fileSyncService;
    private readonly ILogger<WebSocketFileSyncService> _logger;

    private readonly SemaphoreSlim _readLock = new(1);
    private readonly SemaphoreSlim _writeLock = new(1);
    private readonly byte[] _readBuffer = new byte[1024];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public WebSocketFileSyncService(IFileSyncService fileSyncService, ILogger<WebSocketFileSyncService> logger)
    {
        _fileSyncService = fileSyncService;
        _logger = logger;
    }

    public async Task HandleFileSync(WebSocket webSocket, int fileId, CancellationToken cancellationToken)
    {
        string? clientId = null;
        await _fileSyncService.SubscribeToSyncSignalAsync(fileId, async channelMessage =>
        {
            // Don't send the message to the client that updated the file
            if (clientId == channelMessage.ClientId) return;
            
            var message = new FileUpdatedMessage(channelMessage.UpdatedAt, channelMessage.ClientId);
            await WriteMessage(webSocket, message, cancellationToken);
        }, cancellationToken);

        while (webSocket.State == WebSocketState.Open)
        {
            var readResult = await ReadMessage(webSocket, cancellationToken);
            if (readResult.Closed)
                break;
            
            _logger.LogInformation("Received message: {Message}", readResult.Message);

            if (JsonSerializer.Deserialize<ClientIdMessage>(readResult.Message, JsonOptions) is { } clientIdMessage)
            {
                clientId = clientIdMessage.ClientId;
                continue;
            }
            
            _logger.LogInformation("Received message: {Message}", readResult.Message);
        }

        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Sync finished", cancellationToken);
    }

    private async Task<ReadMessageResult> ReadMessage(WebSocket webSocket, CancellationToken cancellationToken)
    {
        await _readLock.WaitAsync(cancellationToken);
        try
        {
            using var ms = new MemoryStream();
            var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(_readBuffer), cancellationToken);
            while (!receiveResult.CloseStatus.HasValue && !receiveResult.EndOfMessage)
            {
                ms.Write(_readBuffer, 0, receiveResult.Count);
                receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(_readBuffer), cancellationToken);
            }

            if (receiveResult.CloseStatus.HasValue)
                return new ReadMessageResult(true, "");
            
            ms.Write(_readBuffer, 0, receiveResult.Count);

            var stringValue = Encoding.UTF8.GetString(ms.ToArray());
            return new ReadMessageResult(false, stringValue);
        }
        finally
        {
            _readLock.Release();
        }
    }

    private async Task WriteMessage(WebSocket webSocket, string message, CancellationToken cancellationToken)
    {
        var messageBytes = Encoding.UTF8.GetBytes(message);
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true,
                cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task WriteMessage<T>(WebSocket webSocket, T message, CancellationToken cancellationToken)
        where T : FileSyncMessage
    {
        var json = JsonSerializer.Serialize(message, JsonOptions);
        await WriteMessage(webSocket, json, cancellationToken);
    }

    private record ReadMessageResult(bool Closed, string Message);

    public void Dispose()
    {
        _readLock.Dispose();
        _writeLock.Dispose();
    }
}
