namespace DataServer.Common.Backoff;

public class RetryConnector
{
    private readonly IBackoffStrategy _backoffStrategy;

    public RetryConnector(IBackoffStrategy backoffStrategy)
    {
        _backoffStrategy = backoffStrategy;
    }

    public async Task ExecuteWithRetryAsync(Func<Task> action, CancellationToken token)
    {
        var attemptNumber = 0;

        while (!token.IsCancellationRequested)
        {
            try
            {
                await action();
                return;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                attemptNumber++;
                var delay = _backoffStrategy.GetDelay(attemptNumber);
                await Task.Delay(delay, token);
            }
        }

        token.ThrowIfCancellationRequested();
    }
}
