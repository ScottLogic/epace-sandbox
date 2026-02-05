namespace DataServer.Application.Interfaces;

public interface IConnectable
{
    bool IsConnected { get; }
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
}
