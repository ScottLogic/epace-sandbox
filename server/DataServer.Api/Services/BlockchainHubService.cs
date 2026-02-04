using System.Text.Json;
using DataServer.Api.Hubs;
using DataServer.Api.Models.JsonRpc;
using DataServer.Application.Logging;
using DataServer.Application.Services;
using DataServer.Common.Extensions;
using DataServer.Domain.Blockchain;
using Microsoft.AspNetCore.SignalR;

namespace DataServer.Api.Services;

public class BlockchainHubService(
    IBlockchainDataService blockchainDataService,
    IHubContext<BlockchainHub> hubContext,
    IAppLogger logger
) : IHostedService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogServiceStarting(nameof(BlockchainHubService));
        blockchainDataService.TradeReceived += OnTradeReceived;
        await blockchainDataService.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogServiceStopping(nameof(BlockchainHubService));
        blockchainDataService.TradeReceived -= OnTradeReceived;
        await blockchainDataService.StopAsync(cancellationToken);
    }

    private void OnTradeReceived(object? sender, TradeUpdate trade)
    {
        _ = BroadcastTradeAsync(trade);
    }

    private async Task BroadcastTradeAsync(TradeUpdate trade)
    {
        try
        {
            var notification = JsonRpcNotification.Create(
                "trades.update",
                new
                {
                    seqnum = trade.Seqnum,
                    @event = "updated",
                    channel = "trades",
                    symbol = trade.Symbol.ToEnumMemberValue(),
                    timestamp = trade.Timestamp,
                    side = trade.Side.ToString().ToLowerInvariant(),
                    qty = trade.Qty,
                    price = trade.Price,
                    tradeId = trade.TradeId,
                }
            );

            var message = JsonSerializer.Serialize(notification, JsonOptions);
            var groupName = BlockchainHub.GetTradesGroupName(trade.Symbol);

            await hubContext.Clients.Group(groupName).SendAsync("ReceiveMessage", message);

            logger.LogTradeBroadcasted(trade.TradeId, trade.Symbol.ToEnumMemberValue(), groupName);
        }
        catch (Exception ex)
        {
            logger.LogBroadcastError(trade.TradeId, ex);
        }
    }
}
