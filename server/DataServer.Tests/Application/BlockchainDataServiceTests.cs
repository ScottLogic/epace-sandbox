using DataServer.Application.Services;
using DataServer.Domain.Blockchain;
using DataServer.Tests.Connectors;
using DataServer.Tests.Infrastructure;

namespace DataServer.Tests.Application;

public class BlockchainDataServiceTests
{
    private readonly FakeBlockchainDataSource _fakeDataSource;
    private readonly FakeBlockchainDataRepository _fakeRepository;
    private readonly BlockchainDataService _service;

    public BlockchainDataServiceTests()
    {
        _fakeDataSource = new FakeBlockchainDataSource();
        _fakeRepository = new FakeBlockchainDataRepository();
        _service = new BlockchainDataService(_fakeDataSource, _fakeRepository);
    }

    [Fact]
    public async Task StartAsync_ConnectsToDataSource()
    {
        await _service.StartAsync();

        Assert.True(_fakeDataSource.IsConnected);
    }

    [Fact]
    public async Task StopAsync_DisconnectsFromDataSource()
    {
        await _service.StartAsync();

        await _service.StopAsync();

        Assert.False(_fakeDataSource.IsConnected);
    }

    [Fact]
    public async Task SubscribeToTradesAsync_DelegatesToDataSource()
    {
        await _service.StartAsync();

        await _service.SubscribeToTradesAsync(Symbol.BtcUsd);

        Assert.Contains(Symbol.BtcUsd, _fakeDataSource.ActiveSubscriptions);
    }

    [Fact]
    public async Task UnsubscribeFromTradesAsync_DelegatesToDataSource()
    {
        await _service.StartAsync();
        await _service.SubscribeToTradesAsync(Symbol.BtcUsd);

        await _service.UnsubscribeFromTradesAsync(Symbol.BtcUsd);

        Assert.DoesNotContain(Symbol.BtcUsd, _fakeDataSource.ActiveSubscriptions);
    }

    [Fact]
    public async Task TradeReceived_StoresTradeInRepository()
    {
        await _service.StartAsync();
        var trade = CreateTestTrade(Symbol.BtcUsd);

        _fakeDataSource.EmitTrade(trade);

        await Task.Delay(50);
        Assert.True(_fakeRepository.StoredTrades.ContainsKey(Symbol.BtcUsd));
        Assert.Single(_fakeRepository.StoredTrades[Symbol.BtcUsd]);
        Assert.Equal(trade, _fakeRepository.StoredTrades[Symbol.BtcUsd][0]);
    }

    [Fact]
    public async Task TradeReceived_ReEmitsEventToConsumers()
    {
        await _service.StartAsync();
        var trade = CreateTestTrade(Symbol.EthUsd);
        TradeUpdate? receivedTrade = null;
        _service.TradeReceived += (sender, t) => receivedTrade = t;

        _fakeDataSource.EmitTrade(trade);

        await Task.Delay(50);
        Assert.NotNull(receivedTrade);
        Assert.Equal(trade, receivedTrade);
    }

    [Fact]
    public async Task GetRecentTradesAsync_ReturnsTradesFromRepository()
    {
        await _service.StartAsync();
        var trade1 = CreateTestTrade(Symbol.BtcUsd, "trade-1");
        var trade2 = CreateTestTrade(Symbol.BtcUsd, "trade-2");
        _fakeDataSource.EmitTrade(trade1);
        _fakeDataSource.EmitTrade(trade2);
        await Task.Delay(50);

        var trades = await _service.GetRecentTradesAsync(Symbol.BtcUsd);

        Assert.Equal(2, trades.Count);
        Assert.Equal(trade1, trades[0]);
        Assert.Equal(trade2, trades[1]);
    }

    [Fact]
    public async Task StopAsync_UnwiresEventHandler()
    {
        await _service.StartAsync();
        await _service.StopAsync();
        var trade = CreateTestTrade(Symbol.BtcUsd);

        _fakeDataSource.EmitTrade(trade);

        await Task.Delay(50);
        Assert.Empty(_fakeRepository.StoredTrades);
    }

    private static TradeUpdate CreateTestTrade(Symbol symbol, string tradeId = "test-trade-1")
    {
        return new TradeUpdate(
            Seqnum: 1,
            Event: Event.Updated,
            Channel: Channel.Trades,
            Symbol: symbol,
            Timestamp: DateTimeOffset.UtcNow,
            Side: Side.Buy,
            Qty: 1.5m,
            Price: 50000m,
            TradeId: tradeId
        );
    }
}
