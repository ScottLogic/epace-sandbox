using DataServer.Domain.Blockchain;

namespace DataServer.Application.Logging;

public interface IApplicationLogger
{
    void LogWebSocketConnecting(Uri uri);
    void LogWebSocketConnected();
    void LogWebSocketDisconnecting();
    void LogWebSocketDisconnected();
    void LogWebSocketClosedByServer();
    void LogWebSocketError(Exception exception);

    void LogHubClientConnected(string connectionId);
    void LogHubClientDisconnected(string connectionId, Exception? exception);

    void LogTradeSubscribed(Symbol symbol);
    void LogTradeUnsubscribed(Symbol symbol);
    void LogClientSubscribed(string connectionId, Symbol symbol);
    void LogClientUnsubscribed(string connectionId, Symbol symbol);
    void LogSubscriptionError(Symbol symbol, Exception exception);
    void LogUnsubscriptionError(Symbol symbol, Exception exception);

    void LogMessageSent(string message);
    void LogMessageReceived(string message);
    void LogMessageParseError(string message, Exception exception);
    void LogJsonRpcParseError(string error);
}
