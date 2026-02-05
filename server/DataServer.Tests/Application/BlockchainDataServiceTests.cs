using DataServer.Application.Interfaces;
using DataServer.Application.Services;
using DataServer.Domain.Blockchain;
using Moq;
using static DataServer.Tests.Shared.TestTradeFactory;

namespace DataServer.Tests.Application;

public class BlockchainDataServiceTests
{
    private readonly Mock<IBlockchainDataClient> _mockDataSource;
    private readonly Mock<IBlockchainDataRepository> _mockRepository;
    private readonly Mock<IConnectionManager> _mockConnectionManager;
    private readonly BlockchainDataService _service;

    public BlockchainDataServiceTests()
    {
        _mockDataSource = new Mock<IBlockchainDataClient>();
        _mockRepository = new Mock<IBlockchainDataRepository>();
        _mockConnectionManager = new Mock<IConnectionManager>();
        _service = new BlockchainDataService(
            _mockDataSource.Object,
            _mockRepository.Object,
            _mockConnectionManager.Object
        );
    }

    [Fact]
    public async Task StartAsync_CallsStartAsyncOnConnectionManager()
    {
        _mockConnectionManager.Verify(
            cm => cm.StartAsync(It.IsAny<CancellationToken>()),
            Times.Never
        );

        await _service.StartAsync();

        _mockConnectionManager.Verify(
            cm => cm.StartAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task StopAsync_CallsStopAsyncOnConnectionManager()
    {
        await _service.StartAsync();

        _mockConnectionManager.Verify(
            cm => cm.StopAsync(It.IsAny<CancellationToken>()),
            Times.Never
        );

        await _service.StopAsync();

        _mockConnectionManager.Verify(
            cm => cm.StopAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task SubscribeToTradesAsync_CallsSubscribeOnDataSourceWithCorrectSymbol()
    {
        const Symbol symbol = Symbol.BtcUsd;

        _mockDataSource.Verify(
            ds => ds.SubscribeToTradesAsync(symbol, It.IsAny<CancellationToken>()),
            Times.Never
        );

        await _service.SubscribeToTradesAsync(symbol);

        _mockDataSource.Verify(
            ds => ds.SubscribeToTradesAsync(symbol, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task UnsubscribeFromTradesAsync_CallsUnsubscribeOnDataSourceWithCorrectSymbol()
    {
        const Symbol symbol = Symbol.BtcUsd;

        _mockDataSource.Verify(
            ds => ds.UnsubscribeFromTradesAsync(symbol, It.IsAny<CancellationToken>()),
            Times.Never
        );

        await _service.UnsubscribeFromTradesAsync(symbol);

        _mockDataSource.Verify(
            ds => ds.UnsubscribeFromTradesAsync(symbol, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task WhenTradeReceived_CallsAddTradeAsyncOnRepository()
    {
        var trade = CreateTestTrade(Symbol.BtcUsd);

        await _service.StartAsync();

        _mockRepository.Verify(
            repo => repo.AddTradeAsync(It.IsAny<TradeUpdate>(), It.IsAny<CancellationToken>()),
            Times.Never
        );

        _mockDataSource.Raise(ds => ds.TradeReceived += null, this, trade);

        await Task.Delay(50);
        _mockRepository.Verify(
            repo => repo.AddTradeAsync(trade, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task WhenTradeReceived_RaisesTradeReceivedEvent()
    {
        var trade = CreateTestTrade(Symbol.EthUsd);
        TradeUpdate? receivedTrade = null;
        _service.TradeReceived += (sender, t) => receivedTrade = t;
        await _service.StartAsync();

        Assert.Null(receivedTrade);

        _mockDataSource.Raise(ds => ds.TradeReceived += null, this, trade);

        await Task.Delay(50);
        Assert.NotNull(receivedTrade);
        Assert.Equal(trade, receivedTrade);
    }

    [Fact]
    public async Task GetRecentTradesAsync_CallsGetRecentTradesAsyncOnRepositoryWithCorrectParameters()
    {
        const Symbol symbol = Symbol.BtcUsd;
        const int count = 50;
        var expectedTrades = new List<TradeUpdate> { CreateTestTrade(symbol) };
        _mockRepository
            .Setup(repo => repo.GetRecentTradesAsync(symbol, count, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTrades);

        _mockRepository.Verify(
            repo => repo.GetRecentTradesAsync(symbol, count, It.IsAny<CancellationToken>()),
            Times.Never
        );

        var result = await _service.GetRecentTradesAsync(symbol, count);

        _mockRepository.Verify(
            repo => repo.GetRecentTradesAsync(symbol, count, It.IsAny<CancellationToken>()),
            Times.Once
        );
        Assert.Equal(expectedTrades, result);
    }

    [Fact]
    public async Task AfterStopAsync_TradeReceivedEventDoesNotCallRepository()
    {
        var tradeBeforeStop = CreateTestTrade(Symbol.BtcUsd, "trade-before-stop");
        var tradeAfterStop = CreateTestTrade(Symbol.BtcUsd, "trade-after-stop");

        _mockRepository.Verify(
            repo => repo.AddTradeAsync(It.IsAny<TradeUpdate>(), It.IsAny<CancellationToken>()),
            Times.Never
        );

        await _service.StartAsync();

        _mockDataSource.Raise(ds => ds.TradeReceived += null, this, tradeBeforeStop);
        await Task.Delay(50);

        _mockRepository.Verify(
            repo => repo.AddTradeAsync(tradeBeforeStop, It.IsAny<CancellationToken>()),
            Times.Once
        );

        await _service.StopAsync();

        _mockDataSource.Raise(ds => ds.TradeReceived += null, this, tradeAfterStop);
        await Task.Delay(50);

        _mockRepository.Verify(
            repo => repo.AddTradeAsync(tradeAfterStop, It.IsAny<CancellationToken>()),
            Times.Never
        );
    }
}
