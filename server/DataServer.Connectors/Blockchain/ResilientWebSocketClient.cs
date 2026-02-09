using System.Net.WebSockets;
using DataServer.Application.Interfaces;
using DataServer.Common.Backoff;

namespace DataServer.Connectors.Blockchain;

public class ResilientWebSocketClient : IWebSocketClient
{
    private readonly RetryConnector _retryConnector;
    private readonly Func<IWebSocketClient> _socketFactory;
    private IWebSocketClient? _inner;

    public ResilientWebSocketClient(RetryConnector retryConnector)
        : this(retryConnector, () => new WebSocketClientWrapper()) { }

    public ResilientWebSocketClient(
        RetryConnector retryConnector,
        Func<IWebSocketClient> socketFactory
    )
    {
        _retryConnector = retryConnector;
        _socketFactory = socketFactory;
    }

    public WebSocketState State => _inner?.State ?? WebSocketState.None;

    public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
    {
        await _retryConnector.ExecuteWithRetryAsync(
            () =>
            {
                _inner?.Dispose();
                var socket = _socketFactory();
                _inner = socket;
                return socket.ConnectAsync(uri, cancellationToken);
            },
            cancellationToken
        );
    }

    public Task CloseAsync(
        WebSocketCloseStatus closeStatus,
        string? statusDescription,
        CancellationToken cancellationToken
    )
    {
        return GetConnectedClient().CloseAsync(closeStatus, statusDescription, cancellationToken);
    }

    public Task SendAsync(
        ArraySegment<byte> buffer,
        WebSocketMessageType messageType,
        bool endOfMessage,
        CancellationToken cancellationToken
    )
    {
        return GetConnectedClient().SendAsync(buffer, messageType, endOfMessage, cancellationToken);
    }

    public Task<WebSocketReceiveResult> ReceiveAsync(
        ArraySegment<byte> buffer,
        CancellationToken cancellationToken
    )
    {
        return GetConnectedClient().ReceiveAsync(buffer, cancellationToken);
    }

    public void Dispose()
    {
        _inner?.Dispose();
    }

    private IWebSocketClient GetConnectedClient()
    {
        return _inner ?? throw new InvalidOperationException("WebSocket is not connected.");
    }
}
