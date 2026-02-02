using DataServer.Application.Interfaces;
using DataServer.Domain.Blockchain;

namespace DataServer.Application.Services;

public class BlockchainDataService : IBlockchainDataService
{
    private readonly IBlockchainDataSource _dataSource;
    private readonly IBlockchainDataRepository _repository;

    public event EventHandler<TradeUpdate>? TradeReceived;

    public BlockchainDataService(
        IBlockchainDataSource dataSource,
        IBlockchainDataRepository repository
    )
    {
        _dataSource = dataSource;
        _repository = repository;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _dataSource.TradeReceived += OnTradeReceived;
        await _dataSource.ConnectAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _dataSource.TradeReceived -= OnTradeReceived;
        await _dataSource.DisconnectAsync(cancellationToken);
    }

    public async Task SubscribeToTradesAsync(
        Symbol symbol,
        CancellationToken cancellationToken = default
    )
    {
        await _dataSource.SubscribeToTradesAsync(symbol, cancellationToken);
    }

    public async Task UnsubscribeFromTradesAsync(
        Symbol symbol,
        CancellationToken cancellationToken = default
    )
    {
        await _dataSource.UnsubscribeFromTradesAsync(symbol, cancellationToken);
    }

    public async Task<IReadOnlyList<TradeUpdate>> GetRecentTradesAsync(
        Symbol symbol,
        int count = 100,
        CancellationToken cancellationToken = default
    )
    {
        return await _repository.GetRecentTradesAsync(symbol, count, cancellationToken);
    }

    private async void OnTradeReceived(object? sender, TradeUpdate trade)
    {
        await _repository.AddTradeAsync(trade);
        TradeReceived?.Invoke(this, trade);
    }
}
