using DataServer.Application.Configuration;
using DataServer.Application.Interfaces;

namespace DataServer.Application.Services;

public class ConnectionManager : IConnectionManager
{
    private readonly IConnectable _connectable;
    private readonly IDelayProvider _delayProvider;
    private readonly ConnectionManagerSettings _settings;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private int _currentDelaySeconds;

    public ConnectionManager(
        IConnectable connectable,
        IDelayProvider delayProvider,
        ConnectionManagerSettings settings
    )
    {
        _connectable = connectable;
        _delayProvider = delayProvider;
        _settings = settings;
        _currentDelaySeconds = settings.InitialDelaySeconds;
    }

    public bool IsConnected => _connectable.IsConnected;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_connectable.IsConnected)
        {
            return;
        }

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_connectable.IsConnected)
            {
                return;
            }

            _currentDelaySeconds = _settings.InitialDelaySeconds;

            while (!cancellationToken.IsCancellationRequested && !_connectable.IsConnected)
            {
                try
                {
                    await _connectable.ConnectAsync(cancellationToken);
                    _currentDelaySeconds = _settings.InitialDelaySeconds;
                }
                catch (Exception) when (!cancellationToken.IsCancellationRequested)
                {
                    await _delayProvider.DelayAsync(
                        TimeSpan.FromSeconds(_currentDelaySeconds),
                        cancellationToken
                    );

                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    _currentDelaySeconds = Math.Min(
                        _currentDelaySeconds * 2,
                        _settings.MaxDelaySeconds
                    );
                }
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_connectable.IsConnected)
        {
            await _connectable.DisconnectAsync(cancellationToken);
        }
    }
}
