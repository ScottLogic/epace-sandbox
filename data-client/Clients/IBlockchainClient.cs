namespace data_client.Clients;

public interface IBlockchainClient
{
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task SubscribeToWebSocketAsync(Func<string, Task> onMessageReceived, CancellationToken cancellationToken = default);
    bool IsConnected { get; }
}
