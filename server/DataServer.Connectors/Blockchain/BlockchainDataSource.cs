using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using DataServer.Application.Configuration;
using DataServer.Application.Interfaces;
using DataServer.Domain.Blockchain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DataServer.Connectors.Blockchain;

public class BlockchainDataSource(
    IOptions<BlockchainSettings> settings,
    IWebSocketClient webSocketClient,
    ILogger<BlockchainDataSource> logger)
    : IBlockchainDataSource, IDisposable
{
    private readonly BlockchainSettings _settings = settings.Value;
    private CancellationTokenSource? _receiveCts;
    private Task? _receiveTask;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public event EventHandler<TradeUpdate>? TradeReceived;
    public event EventHandler<TradeResponse>? SubscriptionConfirmed;

    public bool IsConnected => webSocketClient.State == WebSocketState.Open;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        var uri = new Uri(_settings.ApiUrl);
        logger.LogInformation("Connecting to blockchain API at {Uri}", uri);

        await webSocketClient.ConnectAsync(uri, cancellationToken);

        _receiveCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _receiveTask = ReceiveMessagesAsync(_receiveCts.Token);

        logger.LogInformation("Connected to blockchain API");
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Disconnecting from blockchain API");

        _receiveCts?.Cancel();

        if (webSocketClient.State == WebSocketState.Open)
        {
            await webSocketClient.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Client disconnecting",
                cancellationToken
            );
        }

        if (_receiveTask != null)
        {
            try
            {
                await _receiveTask;
            }
            catch (OperationCanceledException) { }
        }

        logger.LogInformation("Disconnected from blockchain API");
    }

    public async Task SubscribeToTradesAsync(
        Symbol symbol,
        CancellationToken cancellationToken = default
    )
    {
        var request = CreateSubscriptionRequest(SubscriptionAction.Subscribe, symbol);
        await SendMessageAsync(request, cancellationToken);
        logger.LogInformation("Subscribed to trades for {Symbol}", symbol);
    }

    public async Task UnsubscribeFromTradesAsync(
        Symbol symbol,
        CancellationToken cancellationToken = default
    )
    {
        var request = CreateSubscriptionRequest(SubscriptionAction.Unsubscribe, symbol);
        await SendMessageAsync(request, cancellationToken);
        logger.LogInformation("Unsubscribed from trades for {Symbol}", symbol);
    }

    private object CreateSubscriptionRequest(SubscriptionAction action, Symbol symbol)
    {
        var actionString = action == SubscriptionAction.Subscribe ? "subscribe" : "unsubscribe";
        var symbolString = GetSymbolString(symbol);

        if (!string.IsNullOrEmpty(_settings.ApiToken))
        {
            return new
            {
                action = actionString,
                channel = "trades",
                symbol = symbolString,
                token = _settings.ApiToken,
            };
        }

        return new
        {
            action = actionString,
            channel = "trades",
            symbol = symbolString,
        };
    }

    private async Task SendMessageAsync(object message, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(message, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var buffer = new ArraySegment<byte>(bytes);

        await webSocketClient.SendAsync(
            buffer,
            WebSocketMessageType.Text,
            true,
            cancellationToken
        );

        logger.LogDebug("Sent message: {Message}", json);
    }

    private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];

        try
        {
            while (!cancellationToken.IsCancellationRequested && IsConnected)
            {
                var result = await webSocketClient.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    cancellationToken
                );

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    logger.LogInformation("WebSocket closed by server");
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    ProcessMessage(message);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (WebSocketException ex)
        {
            logger.LogError(ex, "WebSocket error while receiving messages");
        }
    }

    private void ProcessMessage(string message)
    {
        try
        {
            logger.LogDebug("Received message: {Message}", message);

            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;

            if (!root.TryGetProperty("event", out var eventElement))
            {
                return;
            }

            var eventString = eventElement.GetString();

            if (eventString == "subscribed" || eventString == "unsubscribed")
            {
                ProcessSubscriptionResponse(root, eventString);
            }
            else if (eventString == "updated")
            {
                ProcessTradeUpdate(root);
            }
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse message: {Message}", message);
        }
    }

    private void ProcessSubscriptionResponse(JsonElement root, string eventString)
    {
        var seqnum = root.TryGetProperty("seqnum", out var seqnumElement)
            ? seqnumElement.GetInt32()
            : 0;
        var symbolString = root.TryGetProperty("symbol", out var symbolElement)
            ? symbolElement.GetString()
            : null;

        var symbol = ParseSymbol(symbolString);
        var eventType = eventString == "subscribed" ? Event.Subscribed : Event.Unsubscribed;

        var response = new TradeResponse(seqnum, eventType, Channel.Trades, symbol);
        SubscriptionConfirmed?.Invoke(this, response);
    }

    private void ProcessTradeUpdate(JsonElement root)
    {
        var seqnum = root.TryGetProperty("seqnum", out var seqnumElement)
            ? seqnumElement.GetInt32()
            : 0;
        var symbolString = root.TryGetProperty("symbol", out var symbolElement)
            ? symbolElement.GetString()
            : null;
        var timestamp = root.TryGetProperty("timestamp", out var timestampElement)
            ? DateTimeOffset.Parse(timestampElement.GetString()!)
            : DateTimeOffset.UtcNow;
        var sideString = root.TryGetProperty("side", out var sideElement)
            ? sideElement.GetString()
            : "buy";
        var qty = root.TryGetProperty("qty", out var qtyElement) ? qtyElement.GetDecimal() : 0m;
        var price = root.TryGetProperty("price", out var priceElement)
            ? priceElement.GetDecimal()
            : 0m;
        var tradeId = root.TryGetProperty("trade_id", out var tradeIdElement)
            ? tradeIdElement.GetString()
            : Guid.NewGuid().ToString();

        var symbol = ParseSymbol(symbolString);
        var side = sideString?.ToLowerInvariant() == "sell" ? Side.Sell : Side.Buy;

        var trade = new TradeUpdate(
            seqnum,
            Event.Updated,
            Channel.Trades,
            symbol,
            timestamp,
            side,
            qty,
            price,
            tradeId!
        );

        TradeReceived?.Invoke(this, trade);
    }

    private static Symbol ParseSymbol(string? symbolString)
    {
        return symbolString?.ToUpperInvariant() switch
        {
            "ETH-USD" => Symbol.EthUsd,
            "BTC-USD" => Symbol.BtcUsd,
            _ => Symbol.BtcUsd,
        };
    }

    private static string GetSymbolString(Symbol symbol)
    {
        return symbol switch
        {
            Symbol.EthUsd => "ETH-USD",
            Symbol.BtcUsd => "BTC-USD",
            _ => symbol.ToString(),
        };
    }

    public void Dispose()
    {
        _receiveCts?.Cancel();
        _receiveCts?.Dispose();
        webSocketClient.Dispose();
    }
}
