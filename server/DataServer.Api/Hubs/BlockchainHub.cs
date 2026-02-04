using System.Text.Json;
using DataServer.Api.Models.JsonRpc;
using DataServer.Application.Logging;
using DataServer.Application.Services;
using DataServer.Common.Extensions;
using DataServer.Domain.Blockchain;
using Microsoft.AspNetCore.SignalR;

namespace DataServer.Api.Hubs;

public class BlockchainHub(IBlockchainDataService blockchainDataService, IApplicationLogger logger)
    : Hub
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public override async Task OnConnectedAsync()
    {
        logger.LogHubClientConnected(Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogHubClientDisconnected(Context.ConnectionId, exception);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string message)
    {
        JsonRpcRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<JsonRpcRequest>(message, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogJsonRpcParseError(ex.Message);
            await SendErrorResponse(JsonRpcError.ParseError(), null);
            return;
        }

        if (request == null || !request.IsValid())
        {
            await SendErrorResponse(JsonRpcError.InvalidRequest(), request?.Id);
            return;
        }

        await HandleRequest(request);
    }

    private async Task HandleRequest(JsonRpcRequest request)
    {
        switch (request.Method.ToLowerInvariant())
        {
            case "subscribe":
                await HandleSubscribe(request);
                break;
            case "unsubscribe":
                await HandleUnsubscribe(request);
                break;
            default:
                await SendErrorResponse(JsonRpcError.MethodNotFound(), request.Id);
                break;
        }
    }

    private async Task HandleSubscribe(JsonRpcRequest request)
    {
        if (request.Params?.Channel?.ToLowerInvariant() != "trades")
        {
            await SendErrorResponse(
                JsonRpcError.InvalidParams("Only 'trades' channel is supported"),
                request.Id
            );
            return;
        }

        if (string.IsNullOrEmpty(request.Params?.Symbol))
        {
            await SendErrorResponse(JsonRpcError.InvalidParams("Symbol is required"), request.Id);
            return;
        }

        if (!request.Params.Symbol.TryParseEnumMember<Symbol>(out var symbol))
        {
            await SendErrorResponse(
                JsonRpcError.InvalidParams($"Invalid symbol: {request.Params.Symbol}"),
                request.Id
            );
            return;
        }

        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetTradesGroupName(symbol));
            await blockchainDataService.SubscribeToTradesAsync(symbol);

            var result = new
            {
                channel = "trades",
                symbol = request.Params.Symbol,
                @event = "subscribed",
            };

            await SendSuccessResponse(result, request.Id);
            logger.LogClientSubscribed(Context.ConnectionId, symbol);
        }
        catch (Exception ex)
        {
            logger.LogSubscriptionError(symbol, ex);
            await SendErrorResponse(JsonRpcError.InternalError(ex.Message), request.Id);
        }
    }

    private async Task HandleUnsubscribe(JsonRpcRequest request)
    {
        if (request.Params?.Channel?.ToLowerInvariant() != "trades")
        {
            await SendErrorResponse(
                JsonRpcError.InvalidParams("Only 'trades' channel is supported"),
                request.Id
            );
            return;
        }

        if (string.IsNullOrEmpty(request.Params?.Symbol))
        {
            await SendErrorResponse(JsonRpcError.InvalidParams("Symbol is required"), request.Id);
            return;
        }

        if (!request.Params.Symbol.TryParseEnumMember<Symbol>(out var symbol))
        {
            await SendErrorResponse(
                JsonRpcError.InvalidParams($"Invalid symbol: {request.Params.Symbol}"),
                request.Id
            );
            return;
        }

        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetTradesGroupName(symbol));
            await blockchainDataService.UnsubscribeFromTradesAsync(symbol);

            var result = new
            {
                channel = "trades",
                symbol = request.Params.Symbol,
                @event = "unsubscribed",
            };

            await SendSuccessResponse(result, request.Id);
            logger.LogClientUnsubscribed(Context.ConnectionId, symbol);
        }
        catch (Exception ex)
        {
            logger.LogUnsubscriptionError(symbol, ex);
            await SendErrorResponse(JsonRpcError.InternalError(ex.Message), request.Id);
        }
    }

    private async Task SendSuccessResponse(object result, string? id)
    {
        var response = JsonRpcResponse.Success(result, id);
        await Clients.Caller.SendAsync(
            "ReceiveMessage",
            JsonSerializer.Serialize(response, JsonOptions)
        );
    }

    private async Task SendErrorResponse(JsonRpcError error, string? id)
    {
        var response = JsonRpcResponse.Failure(error, id);
        await Clients.Caller.SendAsync(
            "ReceiveMessage",
            JsonSerializer.Serialize(response, JsonOptions)
        );
    }

    public static string GetTradesGroupName(Symbol symbol) => $"trades:{symbol}";
}
