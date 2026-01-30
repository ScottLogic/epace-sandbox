using data_client.Clients;

namespace data_client.Services;

public class BlockchainService : IBlockchainService
{
    private readonly IBlockchainClient _blockchainClient;
    private readonly ILogger<BlockchainService> _logger;
    private CancellationTokenSource? _cancellationTokenSource;

    public event EventHandler<string>? OnBlockchainDataReceived;

    public bool IsConnected => _blockchainClient.IsConnected;

    public BlockchainService(IBlockchainClient blockchainClient, ILogger<BlockchainService> logger)
    {
        _blockchainClient = blockchainClient;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting BlockchainService");
        
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        await _blockchainClient.ConnectAsync(_cancellationTokenSource.Token);
        
        _ = Task.Run(async () =>
        {
            try
            {
                await _blockchainClient.SubscribeToWebSocketAsync(HandleWebSocketMessageAsync, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("WebSocket subscription cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WebSocket subscription");
            }
        }, _cancellationTokenSource.Token);
        
        _logger.LogInformation("BlockchainService started");
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping BlockchainService");
        
        _cancellationTokenSource?.Cancel();
        await _blockchainClient.DisconnectAsync(cancellationToken);
        
        _logger.LogInformation("BlockchainService stopped");
    }

    public Task HandleWebSocketMessageAsync(string message)
    {
        _logger.LogDebug("Received blockchain data: {Message}", message);
        
        OnBlockchainDataReceived?.Invoke(this, message);
        
        return Task.CompletedTask;
    }
}
