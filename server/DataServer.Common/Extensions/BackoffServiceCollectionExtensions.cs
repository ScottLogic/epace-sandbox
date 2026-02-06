using DataServer.Common.Backoff;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DataServer.Common.Extensions;

public static class BackoffServiceCollectionExtensions
{
    public static IServiceCollection AddBackoffStrategy(this IServiceCollection services)
    {
        services.AddSingleton<IBackoffStrategy>(sp =>
        {
            var o = sp.GetRequiredService<IOptions<BackoffOptions>>().Value;

            return o.Strategy switch
            {
                BackoffType.Exponential => new ExponentialBackoffStrategy(o),
                BackoffType.Linear => new LinearBackoffStrategy(o),
                _ => throw new NotSupportedException(
                    $"Backoff strategy '{o.Strategy}' is not supported."
                ),
            };
        });

        services.AddSingleton<RetryConnector>();

        return services;
    }
}
