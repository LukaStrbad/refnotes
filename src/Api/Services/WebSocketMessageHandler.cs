using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Api.Model;

namespace Api.Services;

public sealed class WebSocketMessageHandler : IWebSocketMessageHandler, IDisposable
{
    private readonly SemaphoreSlim _readLock = new(1);
    private readonly SemaphoreSlim _writeLock = new(1);
    private readonly byte[] _readBuffer = new byte[1024];

    public async Task<ReadMessageResult> ReadMessage(WebSocket webSocket, CancellationToken cancellationToken)
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

    public async Task WriteMessage(WebSocket webSocket, string message, CancellationToken cancellationToken)
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

    public async Task WriteMessage<T>(WebSocket webSocket, T message, JsonSerializerOptions jsonOptions,
        CancellationToken cancellationToken)
        where T : FileSyncMessage
    {
        var json = JsonSerializer.Serialize(message, jsonOptions);
        await WriteMessage(webSocket, json, cancellationToken);
    }

    public void Dispose()
    {
        _readLock.Dispose();
        _writeLock.Dispose();
    }
}
