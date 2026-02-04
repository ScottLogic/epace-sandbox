using DataServer.Application.Logging;
using DataServer.Domain.Blockchain;
using Microsoft.Extensions.Logging;
using Moq;

namespace DataServer.Tests.Common.Logging;

public class ApplicationLoggerTests
{
    private readonly Mock<ILogger<ApplicationLogger>> _mockLogger;
    private readonly ApplicationLogger _applicationLogger;

    public ApplicationLoggerTests()
    {
        _mockLogger = new Mock<ILogger<ApplicationLogger>>();
        _applicationLogger = new ApplicationLogger(_mockLogger.Object);
    }

    [Fact]
    public void LogWebSocketConnecting_LogsInformationWithUri()
    {
        var uri = new Uri("wss://example.com/api");

        _applicationLogger.LogWebSocketConnecting(uri);

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("wss://example.com/api")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public void LogWebSocketConnected_LogsInformation()
    {
        _applicationLogger.LogWebSocketConnected();

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) =>
                            v.ToString()!.Contains("connected", StringComparison.OrdinalIgnoreCase)
                    ),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public void LogWebSocketDisconnecting_LogsInformation()
    {
        _applicationLogger.LogWebSocketDisconnecting();

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) =>
                            v.ToString()!
                                .Contains("disconnecting", StringComparison.OrdinalIgnoreCase)
                    ),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public void LogWebSocketDisconnected_LogsInformation()
    {
        _applicationLogger.LogWebSocketDisconnected();

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) =>
                            v.ToString()!
                                .Contains("disconnected", StringComparison.OrdinalIgnoreCase)
                    ),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public void LogWebSocketClosedByServer_LogsInformation()
    {
        _applicationLogger.LogWebSocketClosedByServer();

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("closed by server")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public void LogWebSocketError_LogsErrorWithException()
    {
        var exception = new Exception("Test error");

        _applicationLogger.LogWebSocketError(exception);

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public void LogHubClientConnected_LogsInformationWithConnectionId()
    {
        var connectionId = "test-connection-123";

        _applicationLogger.LogHubClientConnected(connectionId);

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(connectionId)),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public void LogHubClientDisconnected_LogsInformationWithConnectionId()
    {
        var connectionId = "test-connection-123";

        _applicationLogger.LogHubClientDisconnected(connectionId, null);

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(connectionId)),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public void LogHubClientDisconnected_WithException_LogsExceptionMessage()
    {
        var connectionId = "test-connection-123";
        var exception = new Exception("Connection lost");

        _applicationLogger.LogHubClientDisconnected(connectionId, exception);

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) =>
                            v.ToString()!.Contains(connectionId)
                            && v.ToString()!.Contains("Connection lost")
                    ),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public void LogTradeSubscribed_LogsInformationWithSymbol()
    {
        var symbol = Symbol.BtcUsd;

        _applicationLogger.LogTradeSubscribed(symbol);

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("BtcUsd")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public void LogTradeUnsubscribed_LogsInformationWithSymbol()
    {
        var symbol = Symbol.EthUsd;

        _applicationLogger.LogTradeUnsubscribed(symbol);

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("EthUsd")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public void LogClientSubscribed_LogsInformationWithConnectionIdAndSymbol()
    {
        var connectionId = "test-connection-123";
        var symbol = Symbol.BtcUsd;

        _applicationLogger.LogClientSubscribed(connectionId, symbol);

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) =>
                            v.ToString()!.Contains(connectionId) && v.ToString()!.Contains("BtcUsd")
                    ),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public void LogClientUnsubscribed_LogsInformationWithConnectionIdAndSymbol()
    {
        var connectionId = "test-connection-123";
        var symbol = Symbol.EthUsd;

        _applicationLogger.LogClientUnsubscribed(connectionId, symbol);

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) =>
                            v.ToString()!.Contains(connectionId) && v.ToString()!.Contains("EthUsd")
                    ),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public void LogSubscriptionError_LogsErrorWithSymbolAndException()
    {
        var symbol = Symbol.BtcUsd;
        var exception = new Exception("Subscription failed");

        _applicationLogger.LogSubscriptionError(symbol, exception);

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("BtcUsd")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public void LogUnsubscriptionError_LogsErrorWithSymbolAndException()
    {
        var symbol = Symbol.EthUsd;
        var exception = new Exception("Unsubscription failed");

        _applicationLogger.LogUnsubscriptionError(symbol, exception);

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("EthUsd")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public void LogMessageSent_LogsDebugWithMessage()
    {
        var message = "{\"action\":\"subscribe\"}";

        _applicationLogger.LogMessageSent(message);

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public void LogMessageReceived_LogsDebugWithMessage()
    {
        var message = "{\"event\":\"subscribed\"}";

        _applicationLogger.LogMessageReceived(message);

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public void LogMessageParseError_LogsWarningWithMessageAndException()
    {
        var message = "invalid json";
        var exception = new Exception("Parse error");

        _applicationLogger.LogMessageParseError(message, exception);

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public void LogJsonRpcParseError_LogsWarningWithError()
    {
        var error = "Invalid JSON format";

        _applicationLogger.LogJsonRpcParseError(error);

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(error)),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }
}
