using DataServer.Domain.Blockchain;
using Microsoft.Extensions.Logging;

namespace DataServer.Application.Logging;

public class ApplicationLogger : IApplicationLogger
{
    private readonly ILogger<ApplicationLogger> _logger;

    public ApplicationLogger(ILogger<ApplicationLogger> logger)
    {
        _logger = logger;
    }

    public void LogWebSocketConnecting(Uri uri)
    {
        _logger.LogInformation("WebSocket connecting to {Uri}", uri);
    }

    public void LogWebSocketConnected()
    {
        _logger.LogInformation("WebSocket connected");
    }

    public void LogWebSocketDisconnecting()
    {
        _logger.LogInformation("WebSocket disconnecting");
    }

    public void LogWebSocketDisconnected()
    {
        _logger.LogInformation("WebSocket disconnected");
    }

    public void LogWebSocketClosedByServer()
    {
        _logger.LogInformation("WebSocket closed by server");
    }

    public void LogWebSocketError(Exception exception)
    {
        _logger.LogError(exception, "WebSocket error occurred");
    }

    public void LogHubClientConnected(string connectionId)
    {
        _logger.LogInformation("Hub client connected: {ConnectionId}", connectionId);
    }

    public void LogHubClientDisconnected(string connectionId, Exception? exception)
    {
        _logger.LogInformation(
            "Hub client disconnected: {ConnectionId}, Exception: {Exception}",
            connectionId,
            exception?.Message
        );
    }

    public void LogTradeSubscribed(Symbol symbol)
    {
        _logger.LogInformation("Subscribed to trades for {Symbol}", symbol);
    }

    public void LogTradeUnsubscribed(Symbol symbol)
    {
        _logger.LogInformation("Unsubscribed from trades for {Symbol}", symbol);
    }

    public void LogClientSubscribed(string connectionId, Symbol symbol)
    {
        _logger.LogInformation(
            "Client {ConnectionId} subscribed to trades for {Symbol}",
            connectionId,
            symbol
        );
    }

    public void LogClientUnsubscribed(string connectionId, Symbol symbol)
    {
        _logger.LogInformation(
            "Client {ConnectionId} unsubscribed from trades for {Symbol}",
            connectionId,
            symbol
        );
    }

    public void LogSubscriptionError(Symbol symbol, Exception exception)
    {
        _logger.LogError(exception, "Failed to subscribe to trades for {Symbol}", symbol);
    }

    public void LogUnsubscriptionError(Symbol symbol, Exception exception)
    {
        _logger.LogError(exception, "Failed to unsubscribe from trades for {Symbol}", symbol);
    }

    public void LogMessageSent(string message)
    {
        _logger.LogDebug("Sent message: {Message}", message);
    }

    public void LogMessageReceived(string message)
    {
        _logger.LogDebug("Received message: {Message}", message);
    }

    public void LogMessageParseError(string message, Exception exception)
    {
        _logger.LogWarning(exception, "Failed to parse message: {Message}", message);
    }

    public void LogJsonRpcParseError(string error)
    {
        _logger.LogWarning("Failed to parse JSON-RPC request: {Error}", error);
    }
}
