using Microsoft.Extensions.Logging;

namespace DataServer.Application.Logging;

public class AppLogger(ILogger<AppLogger> logger) : IAppLogger
{
    public void LogWebsocketConnecting(Uri uri) =>
        logger.LogInformation("Connecting to WebSocket at {Uri}", uri);

    public void LogWebsocketConnected(Uri uri) =>
        logger.LogInformation("Connected to WebSocket at {Uri}", uri);

    public void LogWebsocketReconnecting(Uri uri) =>
        logger.LogInformation("Reconnecting to WebSocket at {Uri}", uri);

    public void LogWebsocketDisconnected(Uri uri, Exception? ex) =>
        logger.LogInformation(
            "Disconnected from WebSocket at {Uri}. Exception: {Exception}",
            uri,
            ex?.Message
        );

    public void LogWebsocketClosedByServer() => logger.LogInformation("WebSocket closed by server");

    public void LogWebsocketError(Exception ex) => logger.LogError(ex, "WebSocket error occurred");

    public void LogSignalRClientConnected(string connectionId) =>
        logger.LogInformation("SignalR client connected: {ConnectionId}", connectionId);

    public void LogSignalRClientDisconnected(string connectionId, Exception? ex) =>
        logger.LogInformation(
            "SignalR client disconnected: {ConnectionId}. Exception: {Exception}",
            connectionId,
            ex?.Message
        );

    public void LogTradeSubscribed(string symbol) =>
        logger.LogInformation("Subscribed to trades for {Symbol}", symbol);

    public void LogTradeUnsubscribed(string symbol) =>
        logger.LogInformation("Unsubscribed from trades for {Symbol}", symbol);

    public void LogClientSubscribed(string connectionId, string symbol) =>
        logger.LogInformation(
            "Client {ConnectionId} subscribed to trades for {Symbol}",
            connectionId,
            symbol
        );

    public void LogClientUnsubscribed(string connectionId, string symbol) =>
        logger.LogInformation(
            "Client {ConnectionId} unsubscribed from trades for {Symbol}",
            connectionId,
            symbol
        );

    public void LogDataStreamReceived(string source, int count) =>
        logger.LogDebug("Received {Count} items from data stream {Source}", count, source);

    public void LogMessageSent(string message) =>
        logger.LogDebug("Sent message: {Message}", message);

    public void LogMessageReceived(string message) =>
        logger.LogDebug("Received message: {Message}", message);

    public void LogProcessingError(string operation, Exception ex) =>
        logger.LogError(ex, "Error during {Operation}", operation);

    public void LogSubscriptionError(string symbol, Exception ex) =>
        logger.LogError(ex, "Failed to subscribe to trades for {Symbol}", symbol);

    public void LogUnsubscriptionError(string symbol, Exception ex) =>
        logger.LogError(ex, "Failed to unsubscribe from trades for {Symbol}", symbol);

    public void LogJsonRpcParseError(string error) =>
        logger.LogWarning("Failed to parse JSON-RPC request: {Error}", error);

    public void LogServiceStarting(string serviceName) =>
        logger.LogInformation("Starting {ServiceName}", serviceName);

    public void LogServiceStopping(string serviceName) =>
        logger.LogInformation("Stopping {ServiceName}", serviceName);

    public void LogTradeBroadcasted(string tradeId, string symbol, string groupName) =>
        logger.LogDebug(
            "Broadcasted trade {TradeId} for {Symbol} to group {GroupName}",
            tradeId,
            symbol,
            groupName
        );

    public void LogBroadcastError(string tradeId, Exception ex) =>
        logger.LogError(ex, "Failed to broadcast trade {TradeId}", tradeId);

    public void LogUnhandledHttpException(
        string method,
        string path,
        int statusCode,
        string traceId,
        Exception ex
    ) =>
        logger.LogError(
            ex,
            "An unhandled exception occurred while processing {Method} {Path}. Status: {StatusCode}, TraceId: {TraceId}",
            method,
            path,
            statusCode,
            traceId
        );

    public void LogHubMethodInvocationFailed(
        string connectionId,
        string methodName,
        Exception ex
    ) =>
        logger.LogError(
            ex,
            "Hub method invocation failed. ConnectionId: {ConnectionId}, Method: {MethodName}, ExceptionType: {ExceptionType}",
            connectionId,
            methodName,
            ex.GetType().Name
        );

    public void LogHubOnConnectedFailed(string connectionId, Exception ex) =>
        logger.LogError(
            ex,
            "OnConnectedAsync failed. ConnectionId: {ConnectionId}, ExceptionType: {ExceptionType}",
            connectionId,
            ex.GetType().Name
        );

    public void LogHubOnDisconnectedFailed(
        string connectionId,
        string? originalException,
        Exception ex
    ) =>
        logger.LogError(
            ex,
            "OnDisconnectedAsync failed. ConnectionId: {ConnectionId}, OriginalException: {OriginalException}, ExceptionType: {ExceptionType}",
            connectionId,
            originalException,
            ex.GetType().Name
        );
}
