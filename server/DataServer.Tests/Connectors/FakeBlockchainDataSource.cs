using DataServer.Application.Interfaces;
using DataServer.Domain.Blockchain;

namespace DataServer.Tests.Connectors;

public class FakeBlockchainDataSource : IBlockchainDataSource
{
    private bool _isConnected;

    public bool IsConnected => _isConnected;

    public HashSet<Symbol> ActiveSubscriptions { get; } = [];

    public event EventHandler<TradeUpdate>? TradeReceived;
    public event EventHandler<TradeResponse>? SubscriptionConfirmed;

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        _isConnected = true;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _isConnected = false;
        return Task.CompletedTask;
    }

    public Task SubscribeToTradesAsync(Symbol symbol, CancellationToken cancellationToken = default)
    {
        ActiveSubscriptions.Add(symbol);

        var response = new TradeResponse(
            Seqnum: 0,
            Event: Event.Subscribed,
            Channel: Channel.Trades,
            Symbol: symbol
        );
        SubscriptionConfirmed?.Invoke(this, response);

        return Task.CompletedTask;
    }

    public Task UnsubscribeFromTradesAsync(
        Symbol symbol,
        CancellationToken cancellationToken = default
    )
    {
        ActiveSubscriptions.Remove(symbol);
        return Task.CompletedTask;
    }

    public void EmitTrade(TradeUpdate trade)
    {
        TradeReceived?.Invoke(this, trade);
    }
}
