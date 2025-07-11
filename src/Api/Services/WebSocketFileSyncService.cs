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
    private readonly Guid _clientId = Guid.NewGuid();

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
        // Send the client ID first
        await WriteMessageObj(webSocket, new UserIdMessage(_clientId.ToString()), cancellationToken);
        
        _ = _fileSyncService.SubscribeToSyncSignalAsync(fileId, async updateTime =>
        {
            var message = new FileUpdatedMessage(updateTime);
            await WriteMessageObj(webSocket, message, cancellationToken);
        }, cancellationToken);


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

    private async Task WriteMessageObj<T>(WebSocket webSocket, T message, CancellationToken cancellationToken)
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
