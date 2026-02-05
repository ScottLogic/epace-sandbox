namespace DataServer.Application.Interfaces;

public interface IConnectionManager
{
    bool IsConnected { get; }
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
