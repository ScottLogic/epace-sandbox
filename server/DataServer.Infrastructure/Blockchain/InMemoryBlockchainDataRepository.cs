using DataServer.Application.Interfaces;
using DataServer.Domain.Blockchain;
using Microsoft.Extensions.Caching.Memory;

namespace DataServer.Infrastructure.Blockchain;

public class InMemoryBlockchainDataRepository : IBlockchainDataRepository
{
    private readonly IMemoryCache _memoryCache;
    private readonly object _lock = new();

    public InMemoryBlockchainDataRepository(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public Task AddTradeAsync(TradeUpdate trade, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(trade.Symbol);

        lock (_lock)
        {
            var cachedTrades = _memoryCache.GetOrCreate(cacheKey, entry => new CachedTrades())!;
            cachedTrades.TryAdd(trade);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TradeUpdate>> GetRecentTradesAsync(
        Symbol symbol,
        int count = 100,
        CancellationToken cancellationToken = default
    )
    {
        var cacheKey = GetCacheKey(symbol);

        lock (_lock)
        {
            if (
                _memoryCache.TryGetValue<CachedTrades>(cacheKey, out var cachedTrades)
                && cachedTrades != null
            )
            {
                return Task.FromResult(cachedTrades.GetRecentTrades(count));
            }
        }

        return Task.FromResult<IReadOnlyList<TradeUpdate>>(Array.Empty<TradeUpdate>());
    }

    public Task ClearTradesAsync(Symbol symbol, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(symbol);

        lock (_lock)
        {
            _memoryCache.Remove(cacheKey);
        }

        return Task.CompletedTask;
    }

    private static string GetCacheKey(Symbol symbol) => $"trades_{symbol}";
}
