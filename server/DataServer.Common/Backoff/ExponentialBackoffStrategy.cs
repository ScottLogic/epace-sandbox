namespace DataServer.Common.Backoff;

public class ExponentialBackoffStrategy : IBackoffStrategy
{
    private readonly BackoffOptions _options;

    public ExponentialBackoffStrategy(BackoffOptions options)
    {
        _options = options;
    }

    public TimeSpan GetDelay(int attemptNumber)
    {
        var delay = TimeSpan.FromTicks(
            (long)(_options.InitialDelay.Ticks * Math.Pow(_options.Multiplier, attemptNumber))
        );

        return delay > _options.MaxDelay ? _options.MaxDelay : delay;
    }
}
