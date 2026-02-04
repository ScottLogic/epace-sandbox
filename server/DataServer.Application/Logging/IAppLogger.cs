namespace DataServer.Application.Logging;

public interface IAppLogger
{
    void LogWebsocketConnecting(Uri uri);
    void LogWebsocketConnected(Uri uri);
    void LogWebsocketReconnecting(Uri uri);
    void LogWebsocketDisconnected(Uri uri, Exception? ex);
    void LogWebsocketClosedByServer();
    void LogWebsocketError(Exception ex);

    void LogSignalRClientConnected(string connectionId);
    void LogSignalRClientDisconnected(string connectionId, Exception? ex);

    void LogTradeSubscribed(string symbol);
    void LogTradeUnsubscribed(string symbol);
    void LogClientSubscribed(string connectionId, string symbol);
    void LogClientUnsubscribed(string connectionId, string symbol);

    void LogDataStreamReceived(string source, int count);

    void LogMessageSent(string message);
    void LogMessageReceived(string message);

    void LogProcessingError(string operation, Exception ex);
    void LogSubscriptionError(string symbol, Exception ex);
    void LogUnsubscriptionError(string symbol, Exception ex);
    void LogJsonRpcParseError(string error);

    void LogServiceStarting(string serviceName);
    void LogServiceStopping(string serviceName);
    void LogTradeBroadcasted(string tradeId, string symbol, string groupName);
    void LogBroadcastError(string tradeId, Exception ex);
}
