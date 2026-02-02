using DataServer.Application.Interfaces;
using DataServer.Domain.Blockchain;

namespace DataServer.Tests.Infrastructure;

public class FakeBlockchainDataRepository : IBlockchainDataRepository
{
    public Dictionary<Symbol, List<TradeUpdate>> StoredTrades { get; } = [];

    public Task AddTradeAsync(TradeUpdate trade, CancellationToken cancellationToken = default)
    {
        if (!StoredTrades.TryGetValue(trade.Symbol, out var trades))
        {
            trades = [];
            StoredTrades[trade.Symbol] = trades;
        }

        trades.Add(trade);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TradeUpdate>> GetRecentTradesAsync(
        Symbol symbol,
        int count = 100,
        CancellationToken cancellationToken = default
    )
    {
        if (!StoredTrades.TryGetValue(symbol, out var trades))
        {
            return Task.FromResult<IReadOnlyList<TradeUpdate>>([]);
        }

        var recentTrades = trades.TakeLast(count).ToList();
        return Task.FromResult<IReadOnlyList<TradeUpdate>>(recentTrades);
    }

    public Task ClearTradesAsync(Symbol symbol, CancellationToken cancellationToken = default)
    {
        StoredTrades.Remove(symbol);
        return Task.CompletedTask;
    }
}
