using Microsoft.AspNetCore.SignalR;

namespace DataServer.Api.Middleware;

public class HubExceptionFilter : IHubFilter
{
    private readonly Serilog.ILogger _logger;

    public HubExceptionFilter(Serilog.ILogger logger)
    {
        _logger = logger;
    }

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next
    )
    {
        try
        {
            return await next(invocationContext);
        }
        catch (Exception ex)
        {
            var connectionId = invocationContext.Context.ConnectionId;
            var methodName = invocationContext.HubMethodName;

            _logger.LogHubMethodInvocationFailed(connectionId, methodName, ex);

            throw;
        }
    }

    public async Task OnConnectedAsync(
        HubLifetimeContext context,
        Func<HubLifetimeContext, Task> next
    )
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var connectionId = context.Context.ConnectionId;

            _logger.LogHubOnConnectedFailed(connectionId, ex);

            throw;
        }
    }

    public async Task OnDisconnectedAsync(
        HubLifetimeContext context,
        Exception? exception,
        Func<HubLifetimeContext, Exception?, Task> next
    )
    {
        try
        {
            await next(context, exception);
        }
        catch (Exception ex)
        {
            var connectionId = context.Context.ConnectionId;

            _logger.LogHubOnDisconnectedFailed(connectionId, exception?.Message, ex);

            throw;
        }
    }
}
