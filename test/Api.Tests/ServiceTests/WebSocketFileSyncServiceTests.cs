using System.Net.WebSockets;
using System.Text.Json;
using Api.Model;
using Api.Services;
using Api.Tests.Data;
using NSubstitute;

namespace Api.Tests.ServiceTests;

public class WebSocketFileSyncServiceTests
{
    private readonly CancellationTokenSource _cts = new();

    [Theory, AutoData]
    public async Task HandleFileSync_SubscribesToFileChanges(
        Sut<WebSocketFileSyncService> sut,
        IFileSyncService fileSyncService,
        IWebSocketMessageHandler messageHandler)
    {
        var webSocket = Substitute.For<WebSocket>();
        const int fileId = 123;
        var channelMessage = new FileSyncChannelMessage(DateTime.Now, Guid.NewGuid().ToString());

        Func<FileSyncChannelMessage, Task>? capturedCallback = null;
        await fileSyncService.SubscribeToSyncSignalAsync(fileId,
            Arg.Do<Func<FileSyncChannelMessage, Task>>(f => capturedCallback = f), _cts.Token);

        await sut.Value.HandleFileSync(webSocket, fileId, _cts.Token);

        // Simulate a message
        Assert.NotNull(capturedCallback);
        await capturedCallback.Invoke(channelMessage);

        await messageHandler.Received(1)
            .WriteMessage(webSocket,
                Arg.Is<FileUpdatedMessage>(msg =>
                    msg.Time == channelMessage.UpdatedAt && msg.SenderClientId == channelMessage.ClientId),
                Arg.Any<JsonSerializerOptions>(), _cts.Token);
    }

    [Theory, AutoData]
    public async Task HandleFileSync_ClosesConnection_WhenClientDisconnects(
        Sut<WebSocketFileSyncService> sut,
        IWebSocketMessageHandler messageHandler)
    {
        var webSocket = Substitute.For<WebSocket>();
        const int fileId = 123;

        // WebSocket is open initially
        webSocket.State.Returns(WebSocketState.Open);
        // Simulate a close message
        messageHandler.ReadMessage(webSocket, _cts.Token).Returns(new ReadMessageResult(true, ""));

        await sut.Value.HandleFileSync(webSocket, fileId, _cts.Token);

        await webSocket.Received(1).CloseAsync(WebSocketCloseStatus.NormalClosure, Arg.Any<string>(), _cts.Token);
    }

    [Theory, AutoData]
    public async Task HandleFileSync_DoesntSendMessages_ToTheSameClient(
        Sut<WebSocketFileSyncService> sut,
        IFileSyncService fileSyncService,
        IWebSocketMessageHandler messageHandler)
    {
        var webSocket = Substitute.For<WebSocket>();
        webSocket.State.Returns(WebSocketState.Open);
        const int fileId = 123;
        var clientId = Guid.NewGuid().ToString();
        var clientIdMessage = new ClientIdMessage(clientId);
        var clientIdMessageJson = JsonSerializer.Serialize(clientIdMessage, WebSocketFileSyncService.JsonOptions);

        var channelMessage = new FileSyncChannelMessage(DateTime.Now, clientId);

        messageHandler.ReadMessage(webSocket, _cts.Token).Returns(
            new ReadMessageResult(false, clientIdMessageJson),
            new ReadMessageResult(true, "")
        );

        Func<FileSyncChannelMessage, Task>? capturedCallback = null;
        await fileSyncService.SubscribeToSyncSignalAsync(fileId,
            Arg.Do<Func<FileSyncChannelMessage, Task>>(f => capturedCallback = f), _cts.Token);

        await sut.Value.HandleFileSync(webSocket, fileId, _cts.Token);

        Assert.NotNull(capturedCallback);
        await capturedCallback.Invoke(channelMessage);

        await messageHandler.DidNotReceive().WriteMessage(webSocket, Arg.Any<FileUpdatedMessage>(),
            Arg.Any<JsonSerializerOptions>(), _cts.Token);
        await webSocket.Received(1).CloseAsync(WebSocketCloseStatus.NormalClosure, Arg.Any<string>(), _cts.Token);
    }
}
