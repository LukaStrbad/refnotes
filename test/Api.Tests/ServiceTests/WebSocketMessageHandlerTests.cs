using System.Net.WebSockets;
using System.Text;
using Api.Services;
using Api.Tests.Data;
using NSubstitute;

namespace Api.Tests.ServiceTests;

public sealed class WebSocketMessageHandlerTests
{
    private readonly CancellationTokenSource _cts = new();

    [Theory, AutoData]
    public async Task ReadMessage_ReadsMessage(Sut<WebSocketMessageHandler> sut)
    {
        var webSocket = Substitute.For<WebSocket>();
        const string message = "test message";
        var messageBytes = Encoding.UTF8.GetBytes(message);

        webSocket.ReceiveAsync(Arg.Do<ArraySegment<byte>>(arraySegment =>
        {
            var array = arraySegment.Array;
            ArgumentNullException.ThrowIfNull(array);
            messageBytes.CopyTo(array, 0);
        }), _cts.Token).Returns(new WebSocketReceiveResult(messageBytes.Length, WebSocketMessageType.Text, true));

        var result = await sut.Value.ReadMessage(webSocket, _cts.Token);

        Assert.False(result.Closed);
        Assert.Equal(message, result.Message);
    }

    [Theory, AutoData]
    public async Task WriteMessage_WritesMessage(Sut<WebSocketMessageHandler> sut)
    {
        var webSocket = Substitute.For<WebSocket>();
        const string message = "test message";
        var messageBytes = Encoding.UTF8.GetBytes(message);

        await sut.Value.WriteMessage(webSocket, message, _cts.Token);

        await webSocket.Received(1).SendAsync(
            Arg.Is<ArraySegment<byte>>(arraySegment => messageBytes.SequenceEqual(arraySegment.ToArray())),
            WebSocketMessageType.Text, true, _cts.Token);
    }
}
