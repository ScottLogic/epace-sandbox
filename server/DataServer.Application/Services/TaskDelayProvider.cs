using DataServer.Application.Interfaces;

namespace DataServer.Application.Services;

public class TaskDelayProvider : IDelayProvider
{
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        return Task.Delay(delay, cancellationToken);
    }
}
