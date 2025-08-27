using System.Net.WebSockets;
using System.Text;
using Api.Services;
using Api.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Api.Tests.ServiceTests;

public sealed class WebSocketMessageHandlerTests
{
    private readonly CancellationTokenSource _cts = new();
    private readonly WebSocketMessageHandler _service;

    public WebSocketMessageHandlerTests()
    {
        var serviceProvider = new ServiceFixture<WebSocketMessageHandler>().CreateServiceProvider();
        _service = serviceProvider.GetRequiredService<WebSocketMessageHandler>();
    }

    [Fact]
    public async Task ReadMessage_ReadsMessage()
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

        var result = await _service.ReadMessage(webSocket, _cts.Token);

        Assert.False(result.Closed);
        Assert.Equal(message, result.Message);
    }

    [Fact]
    public async Task WriteMessage_WritesMessage()
    {
        var webSocket = Substitute.For<WebSocket>();
        const string message = "test message";
        var messageBytes = Encoding.UTF8.GetBytes(message);

        await _service.WriteMessage(webSocket, message, _cts.Token);

        await webSocket.Received(1).SendAsync(
            Arg.Is<ArraySegment<byte>>(arraySegment => messageBytes.SequenceEqual(arraySegment.ToArray())),
            WebSocketMessageType.Text, true, _cts.Token);
    }
}
