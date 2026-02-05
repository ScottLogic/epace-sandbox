using DataServer.Application.Configuration;
using DataServer.Application.Interfaces;
using DataServer.Application.Services;
using Moq;

namespace DataServer.Tests.Application;

public class ConnectionManagerTests
{
    private readonly Mock<IConnectable> _mockConnectable;
    private readonly Mock<IDelayProvider> _mockDelayProvider;
    private readonly ConnectionManagerSettings _settings;

    public ConnectionManagerTests()
    {
        _mockConnectable = new Mock<IConnectable>();
        _mockDelayProvider = new Mock<IDelayProvider>();
        _settings = new ConnectionManagerSettings
        {
            InitialDelaySeconds = 5,
            MaxDelaySeconds = 300,
        };

        _mockDelayProvider
            .Setup(d => d.DelayAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private IConnectionManager CreateConnectionManager()
    {
        return new ConnectionManager(_mockConnectable.Object, _mockDelayProvider.Object, _settings);
    }

    [Fact]
    public async Task StartAsync_WhenNotConnected_AttemptsConnection()
    {
        _mockConnectable.Setup(c => c.IsConnected).Returns(false);
        _mockConnectable
            .Setup(c => c.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(() => _mockConnectable.Setup(c => c.IsConnected).Returns(true));

        var manager = CreateConnectionManager();

        await manager.StartAsync();

        _mockConnectable.Verify(c => c.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyConnected_DoesNotAttemptConnection()
    {
        _mockConnectable.Setup(c => c.IsConnected).Returns(true);

        var manager = CreateConnectionManager();

        await manager.StartAsync();

        _mockConnectable.Verify(c => c.ConnectAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartAsync_WhenConnectionFails_RetriesWithExponentialBackoff()
    {
        var connectionAttempts = 0;
        _mockConnectable.Setup(c => c.IsConnected).Returns(false);
        _mockConnectable
            .Setup(c => c.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                connectionAttempts++;
                if (connectionAttempts < 3)
                {
                    throw new Exception("Connection failed");
                }
                _mockConnectable.Setup(c => c.IsConnected).Returns(true);
                return Task.CompletedTask;
            });

        var delays = new List<TimeSpan>();
        _mockDelayProvider
            .Setup(d => d.DelayAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Callback<TimeSpan, CancellationToken>((delay, _) => delays.Add(delay))
            .Returns(Task.CompletedTask);

        var manager = CreateConnectionManager();

        await manager.StartAsync();

        Assert.Equal(3, connectionAttempts);
        Assert.Equal(2, delays.Count);
        Assert.Equal(TimeSpan.FromSeconds(5), delays[0]);
        Assert.Equal(TimeSpan.FromSeconds(10), delays[1]);
    }

    [Fact]
    public async Task StartAsync_ExponentialBackoff_DoublesEachTime()
    {
        var connectionAttempts = 0;
        _mockConnectable.Setup(c => c.IsConnected).Returns(false);
        _mockConnectable
            .Setup(c => c.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                connectionAttempts++;
                if (connectionAttempts < 5)
                {
                    throw new Exception("Connection failed");
                }
                _mockConnectable.Setup(c => c.IsConnected).Returns(true);
                return Task.CompletedTask;
            });

        var delays = new List<TimeSpan>();
        _mockDelayProvider
            .Setup(d => d.DelayAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Callback<TimeSpan, CancellationToken>((delay, _) => delays.Add(delay))
            .Returns(Task.CompletedTask);

        var manager = CreateConnectionManager();

        await manager.StartAsync();

        Assert.Equal(5, connectionAttempts);
        Assert.Equal(4, delays.Count);
        Assert.Equal(TimeSpan.FromSeconds(5), delays[0]);
        Assert.Equal(TimeSpan.FromSeconds(10), delays[1]);
        Assert.Equal(TimeSpan.FromSeconds(20), delays[2]);
        Assert.Equal(TimeSpan.FromSeconds(40), delays[3]);
    }

    [Fact]
    public async Task StartAsync_ExponentialBackoff_CapsAtMaxDelay()
    {
        var settingsWithLowMax = new ConnectionManagerSettings
        {
            InitialDelaySeconds = 5,
            MaxDelaySeconds = 15,
        };

        var connectionAttempts = 0;
        _mockConnectable.Setup(c => c.IsConnected).Returns(false);
        _mockConnectable
            .Setup(c => c.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                connectionAttempts++;
                if (connectionAttempts < 5)
                {
                    throw new Exception("Connection failed");
                }
                _mockConnectable.Setup(c => c.IsConnected).Returns(true);
                return Task.CompletedTask;
            });

        var delays = new List<TimeSpan>();
        _mockDelayProvider
            .Setup(d => d.DelayAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Callback<TimeSpan, CancellationToken>((delay, _) => delays.Add(delay))
            .Returns(Task.CompletedTask);

        var manager = new ConnectionManager(
            _mockConnectable.Object,
            _mockDelayProvider.Object,
            settingsWithLowMax
        );

        await manager.StartAsync();

        Assert.Equal(TimeSpan.FromSeconds(5), delays[0]);
        Assert.Equal(TimeSpan.FromSeconds(10), delays[1]);
        Assert.Equal(TimeSpan.FromSeconds(15), delays[2]);
        Assert.Equal(TimeSpan.FromSeconds(15), delays[3]);
    }

    [Fact]
    public async Task StartAsync_WhenCancelled_StopsRetrying()
    {
        var connectionAttempts = 0;
        _mockConnectable.Setup(c => c.IsConnected).Returns(false);
        _mockConnectable
            .Setup(c => c.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                connectionAttempts++;
                throw new Exception("Connection failed");
            });

        using var cts = new CancellationTokenSource();
        _mockDelayProvider
            .Setup(d => d.DelayAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Callback<TimeSpan, CancellationToken>((_, _) => cts.Cancel())
            .Returns(Task.CompletedTask);

        var manager = CreateConnectionManager();

        await manager.StartAsync(cts.Token);

        Assert.Equal(1, connectionAttempts);
    }

    [Fact]
    public async Task StopAsync_DisconnectsWhenConnected()
    {
        _mockConnectable.Setup(c => c.IsConnected).Returns(true);
        _mockConnectable
            .Setup(c => c.DisconnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var manager = CreateConnectionManager();

        await manager.StopAsync();

        _mockConnectable.Verify(c => c.DisconnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StopAsync_DoesNotDisconnectWhenNotConnected()
    {
        _mockConnectable.Setup(c => c.IsConnected).Returns(false);

        var manager = CreateConnectionManager();

        await manager.StopAsync();

        _mockConnectable.Verify(c => c.DisconnectAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartAsync_OnlyOneConnectionAttemptAtATime()
    {
        var connectionInProgress = false;
        var concurrentAttempts = 0;
        var maxConcurrentAttempts = 0;

        _mockConnectable.Setup(c => c.IsConnected).Returns(false);
        _mockConnectable
            .Setup(c => c.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                if (connectionInProgress)
                {
                    Interlocked.Increment(ref concurrentAttempts);
                }
                connectionInProgress = true;
                maxConcurrentAttempts = Math.Max(
                    maxConcurrentAttempts,
                    Interlocked.Increment(ref concurrentAttempts)
                );
                await Task.Delay(10);
                Interlocked.Decrement(ref concurrentAttempts);
                connectionInProgress = false;
                _mockConnectable.Setup(c => c.IsConnected).Returns(true);
            });

        var manager = CreateConnectionManager();

        var task1 = manager.StartAsync();
        var task2 = manager.StartAsync();

        await Task.WhenAll(task1, task2);

        Assert.Equal(1, maxConcurrentAttempts);
    }

    [Fact]
    public async Task StartAsync_ResetsBackoffAfterSuccessfulConnection()
    {
        var connectionAttempts = 0;
        var isConnected = false;

        _mockConnectable.Setup(c => c.IsConnected).Returns(() => isConnected);
        _mockConnectable
            .Setup(c => c.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                connectionAttempts++;
                if (connectionAttempts == 1)
                {
                    throw new Exception("Connection failed");
                }
                isConnected = true;
                return Task.CompletedTask;
            });
        _mockConnectable
            .Setup(c => c.DisconnectAsync(It.IsAny<CancellationToken>()))
            .Callback(() => isConnected = false)
            .Returns(Task.CompletedTask);

        var delays = new List<TimeSpan>();
        _mockDelayProvider
            .Setup(d => d.DelayAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Callback<TimeSpan, CancellationToken>((delay, _) => delays.Add(delay))
            .Returns(Task.CompletedTask);

        var manager = CreateConnectionManager();

        await manager.StartAsync();
        Assert.True(isConnected);
        Assert.Single(delays);
        Assert.Equal(TimeSpan.FromSeconds(5), delays[0]);

        await manager.StopAsync();
        Assert.False(isConnected);

        delays.Clear();
        connectionAttempts = 0;

        _mockConnectable
            .Setup(c => c.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                connectionAttempts++;
                if (connectionAttempts == 1)
                {
                    throw new Exception("Connection failed");
                }
                isConnected = true;
                return Task.CompletedTask;
            });

        await manager.StartAsync();

        Assert.Single(delays);
        Assert.Equal(TimeSpan.FromSeconds(5), delays[0]);
    }

    [Fact]
    public void IsConnected_ReturnsConnectableIsConnected()
    {
        _mockConnectable.Setup(c => c.IsConnected).Returns(true);
        var manager = CreateConnectionManager();

        Assert.True(manager.IsConnected);

        _mockConnectable.Setup(c => c.IsConnected).Returns(false);

        Assert.False(manager.IsConnected);
    }
}
