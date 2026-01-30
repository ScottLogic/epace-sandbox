using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Options;

namespace data_client.Clients;

public class BlockchainClientOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string WebSocketEndpoint { get; set; } = "/ws";
}

public class BlockchainClient : IBlockchainClient, IDisposable
{
    private readonly BlockchainClientOptions _options;
    private readonly ILogger<BlockchainClient> _logger;
    private ClientWebSocket? _webSocket;
    private bool _disposed;

    public BlockchainClient(IOptions<BlockchainClientOptions> options, ILogger<BlockchainClient> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConnected => _webSocket?.State == WebSocketState.Open;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_webSocket != null && _webSocket.State == WebSocketState.Open)
        {
            return;
        }

        _webSocket = new ClientWebSocket();
        var wsUri = new Uri($"{_options.BaseUrl.Replace("http", "ws")}{_options.WebSocketEndpoint}");
        
        _logger.LogInformation("Connecting to blockchain WebSocket at {Uri}", wsUri);
        await _webSocket.ConnectAsync(wsUri, cancellationToken);
        _logger.LogInformation("Connected to blockchain WebSocket");
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
        {
            return;
        }

        _logger.LogInformation("Disconnecting from blockchain WebSocket");
        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
        _logger.LogInformation("Disconnected from blockchain WebSocket");
    }

    public async Task SubscribeToWebSocketAsync(Func<string, Task> onMessageReceived, CancellationToken cancellationToken = default)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket is not connected. Call ConnectAsync first.");
        }

        var buffer = new byte[4096];
        var messageBuilder = new StringBuilder();

        while (_webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                _logger.LogInformation("WebSocket connection closed by server");
                break;
            }

            messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

            if (result.EndOfMessage)
            {
                var message = messageBuilder.ToString();
                messageBuilder.Clear();
                
                _logger.LogDebug("Received WebSocket message: {Message}", message);
                await onMessageReceived(message);
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _webSocket?.Dispose();
        }

        _disposed = true;
    }
}
