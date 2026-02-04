using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using DataServer.Application.Configuration;
using DataServer.Application.Interfaces;
using DataServer.Application.Logging;
using DataServer.Common.Extensions;
using DataServer.Domain.Blockchain;
using Microsoft.Extensions.Options;

namespace DataServer.Connectors.Blockchain;

public class BlockchainDataClient(
    IOptions<BlockchainSettings> options,
    IWebSocketClient webSocketClient,
    IAppLogger logger
) : IBlockchainDataClient, IDisposable
{
    private readonly BlockchainSettings _options = options.Value;
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
        var uri = new Uri(_options.ApiUrl);
        logger.LogWebsocketConnecting(uri);

        await webSocketClient.ConnectAsync(uri, cancellationToken);

        _receiveCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _receiveTask = ReceiveMessagesAsync(_receiveCts.Token);

        logger.LogWebsocketConnected(uri);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        var uri = new Uri(_options.ApiUrl);

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

        logger.LogWebsocketDisconnected(uri, null);
    }

    public async Task SubscribeToTradesAsync(
        Symbol symbol,
        CancellationToken cancellationToken = default
    )
    {
        var request = CreateSubscriptionRequest(SubscriptionAction.Subscribe, symbol);
        await SendMessageAsync(request, cancellationToken);
        logger.LogTradeSubscribed(symbol.ToEnumMemberValue());
    }

    public async Task UnsubscribeFromTradesAsync(
        Symbol symbol,
        CancellationToken cancellationToken = default
    )
    {
        var request = CreateSubscriptionRequest(SubscriptionAction.Unsubscribe, symbol);
        await SendMessageAsync(request, cancellationToken);
        logger.LogTradeUnsubscribed(symbol.ToEnumMemberValue());
    }

    private object CreateSubscriptionRequest(SubscriptionAction action, Symbol symbol)
    {
        var actionString = action == SubscriptionAction.Subscribe ? "subscribe" : "unsubscribe";
        var symbolString = symbol.ToEnumMemberValue();

        if (!string.IsNullOrEmpty(_options.ApiToken))
        {
            return new
            {
                action = actionString,
                channel = "trades",
                symbol = symbolString,
                token = _options.ApiToken,
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

        await webSocketClient.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken);

        logger.LogMessageSent(json);
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
                    logger.LogWebsocketClosedByServer();
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
            logger.LogWebsocketError(ex);
        }
    }

    private void ProcessMessage(string message)
    {
        try
        {
            logger.LogMessageReceived(message);

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
            logger.LogProcessingError($"Failed to parse message: {message}", ex);
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

        if (symbolString == null)
        {
            return;
        }

        symbolString.TryParseEnumMember<Symbol>(out var symbol);

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

        if (symbolString == null)
        {
            return;
        }

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

        symbolString.TryParseEnumMember<Symbol>(out var symbol);

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

    public void Dispose()
    {
        _receiveCts?.Cancel();
        _receiveCts?.Dispose();
        webSocketClient.Dispose();
    }
}
