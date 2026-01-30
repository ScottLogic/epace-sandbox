namespace data_client.Services;

public interface IBlockchainService
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task HandleWebSocketMessageAsync(string message);
    event EventHandler<string>? OnBlockchainDataReceived;
}
