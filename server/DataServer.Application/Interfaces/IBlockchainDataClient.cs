using DataServer.Domain.Blockchain;

namespace DataServer.Application.Interfaces;

public interface IBlockchainDataClient : IConnectable
{
    Task SubscribeToTradesAsync(Symbol symbol, CancellationToken cancellationToken = default);
    Task UnsubscribeFromTradesAsync(Symbol symbol, CancellationToken cancellationToken = default);

    event EventHandler<TradeUpdate>? TradeReceived;
    event EventHandler<TradeResponse>? SubscriptionConfirmed;
}
