using Microsoft.AspNetCore.SignalR;

namespace DataServer.Api.Middleware;

public class HubExceptionFilter : IHubFilter
{
    private readonly ILogger<HubExceptionFilter> _logger;

    public HubExceptionFilter(ILogger<HubExceptionFilter> logger)
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

            _logger.LogError(
                ex,
                "Hub method invocation failed. ConnectionId: {ConnectionId}, Method: {MethodName}, ExceptionType: {ExceptionType}",
                connectionId,
                methodName,
                ex.GetType().Name
            );

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

            _logger.LogError(
                ex,
                "OnConnectedAsync failed. ConnectionId: {ConnectionId}, ExceptionType: {ExceptionType}",
                connectionId,
                ex.GetType().Name
            );

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

            _logger.LogError(
                ex,
                "OnDisconnectedAsync failed. ConnectionId: {ConnectionId}, OriginalException: {OriginalException}, ExceptionType: {ExceptionType}",
                connectionId,
                exception?.Message,
                ex.GetType().Name
            );

            throw;
        }
    }
}
