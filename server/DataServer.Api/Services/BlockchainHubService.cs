using System.Text.Json;
using DataServer.Api.Hubs;
using DataServer.Api.Models.JsonRpc;
using DataServer.Application.Services;
using DataServer.Common.Extensions;
using DataServer.Domain.Blockchain;
using Microsoft.AspNetCore.SignalR;

namespace DataServer.Api.Services;

public class BlockchainHubService(
    IBlockchainDataService blockchainDataService,
    IHubContext<BlockchainHub> hubContext,
    ILogger<BlockchainHubService> logger)
    : IHostedService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting BlockchainHubService");
        blockchainDataService.TradeReceived += OnTradeReceived;
        await blockchainDataService.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping BlockchainHubService");
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

            logger.LogDebug(
                "Broadcasted trade {TradeId} for {Symbol} to group {GroupName}",
                trade.TradeId,
                trade.Symbol,
                groupName
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to broadcast trade {TradeId}", trade.TradeId);
        }
    }
}
