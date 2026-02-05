namespace DataServer.Application.Interfaces;

public interface IDelayProvider
{
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default);
}
