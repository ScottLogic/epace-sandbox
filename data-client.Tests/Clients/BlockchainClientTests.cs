using data_client.Clients;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace data_client.Tests.Clients;

public class BlockchainClientTests
{
    private readonly Mock<ILogger<BlockchainClient>> _loggerMock;
    private readonly BlockchainClientOptions _options;

    public BlockchainClientTests()
    {
        _loggerMock = new Mock<ILogger<BlockchainClient>>();
        _options = new BlockchainClientOptions
        {
            BaseUrl = "http://localhost:5001",
            WebSocketEndpoint = "/ws"
        };
    }

    private BlockchainClient CreateClient(BlockchainClientOptions? options = null)
    {
        var opts = Options.Create(options ?? _options);
        return new BlockchainClient(opts, _loggerMock.Object);
    }

    #region IsConnected Tests

    [Fact]
    public void IsConnected_WhenNotConnected_ReturnsFalse()
    {
        using var client = CreateClient();

        var result = client.IsConnected;

        Assert.False(result);
    }

    [Fact]
    public void IsConnected_WhenNewlyCreated_ReturnsFalse()
    {
        using var client = CreateClient();

        Assert.False(client.IsConnected);
    }

    #endregion

    #region ConnectAsync Tests

    [Fact]
    public async Task ConnectAsync_WithInvalidUrl_ThrowsException()
    {
        var invalidOptions = new BlockchainClientOptions
        {
            BaseUrl = "invalid-url",
            WebSocketEndpoint = "/ws"
        };
        using var client = CreateClient(invalidOptions);

        await Assert.ThrowsAsync<UriFormatException>(() => client.ConnectAsync());
    }

    [Fact]
    public async Task ConnectAsync_WithEmptyBaseUrl_ThrowsArgumentException()
    {
        var emptyOptions = new BlockchainClientOptions
        {
            BaseUrl = "",
            WebSocketEndpoint = "/ws"
        };
        using var client = CreateClient(emptyOptions);

        await Assert.ThrowsAsync<ArgumentException>(() => client.ConnectAsync());
    }

    [Fact]
    public async Task ConnectAsync_WithCancellationRequested_ThrowsTaskCanceledException()
    {
        using var client = CreateClient();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => client.ConnectAsync(cts.Token));
    }

    [Fact]
    public async Task ConnectAsync_WithUnreachableServer_ThrowsWebSocketException()
    {
        var unreachableOptions = new BlockchainClientOptions
        {
            BaseUrl = "http://localhost:59999",
            WebSocketEndpoint = "/ws"
        };
        using var client = CreateClient(unreachableOptions);

        await Assert.ThrowsAsync<System.Net.WebSockets.WebSocketException>(() => client.ConnectAsync());
    }

    #endregion

    #region DisconnectAsync Tests

    [Fact]
    public async Task DisconnectAsync_WhenNotConnected_DoesNotThrow()
    {
        using var client = CreateClient();

        var exception = await Record.ExceptionAsync(() => client.DisconnectAsync());

        Assert.Null(exception);
    }

    [Fact]
    public async Task DisconnectAsync_WhenNeverConnected_CompletesSuccessfully()
    {
        using var client = CreateClient();

        await client.DisconnectAsync();

        Assert.False(client.IsConnected);
    }

    [Fact]
    public async Task DisconnectAsync_CalledMultipleTimes_DoesNotThrow()
    {
        using var client = CreateClient();

        await client.DisconnectAsync();
        await client.DisconnectAsync();
        await client.DisconnectAsync();

        Assert.False(client.IsConnected);
    }

    #endregion

    #region SubscribeToWebSocketAsync Tests

    [Fact]
    public async Task SubscribeToWebSocketAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        using var client = CreateClient();
        Func<string, Task> callback = _ => Task.CompletedTask;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.SubscribeToWebSocketAsync(callback));

        Assert.Equal("WebSocket is not connected. Call ConnectAsync first.", exception.Message);
    }

    [Fact]
    public async Task SubscribeToWebSocketAsync_WithNullCallback_WhenNotConnected_ThrowsInvalidOperationException()
    {
        using var client = CreateClient();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.SubscribeToWebSocketAsync(null!));
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_WhenNotConnected_DoesNotThrow()
    {
        var client = CreateClient();

        var exception = Record.Exception(() => client.Dispose());

        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        var client = CreateClient();

        client.Dispose();
        client.Dispose();
        client.Dispose();
    }

    [Fact]
    public void Dispose_AfterDispose_IsConnectedReturnsFalse()
    {
        var client = CreateClient();

        client.Dispose();

        Assert.False(client.IsConnected);
    }

    #endregion

    #region BlockchainClientOptions Tests

    [Fact]
    public void BlockchainClientOptions_DefaultValues_AreCorrect()
    {
        var options = new BlockchainClientOptions();

        Assert.Equal(string.Empty, options.BaseUrl);
        Assert.Equal("/ws", options.WebSocketEndpoint);
    }

    [Fact]
    public void BlockchainClientOptions_CanSetProperties()
    {
        var options = new BlockchainClientOptions
        {
            BaseUrl = "http://example.com",
            WebSocketEndpoint = "/custom-ws"
        };

        Assert.Equal("http://example.com", options.BaseUrl);
        Assert.Equal("/custom-ws", options.WebSocketEndpoint);
    }

    #endregion

    #region URL Conversion Tests

    [Fact]
    public async Task ConnectAsync_ConvertsHttpToWs_InUrl()
    {
        var options = new BlockchainClientOptions
        {
            BaseUrl = "http://localhost:5001",
            WebSocketEndpoint = "/ws"
        };
        using var client = CreateClient(options);

        await Assert.ThrowsAsync<System.Net.WebSockets.WebSocketException>(() => client.ConnectAsync());

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ws://localhost:5001/ws")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ConnectAsync_ConvertsHttpsToWss_InUrl()
    {
        var options = new BlockchainClientOptions
        {
            BaseUrl = "https://localhost:5001",
            WebSocketEndpoint = "/ws"
        };
        using var client = CreateClient(options);

        await Assert.ThrowsAsync<System.Net.WebSockets.WebSocketException>(() => client.ConnectAsync());

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("wss://localhost:5001/ws")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}
