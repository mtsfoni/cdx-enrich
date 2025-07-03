using System.Threading.RateLimiting;
using Microsoft.Extensions.Logging;

namespace CdxEnrich.ClearlyDefined
{
    /// <summary>
    /// Manages rate limiting and concurrency limiting for HTTP requests.
    /// </summary>
    public class RequestLimiter
    {
        private readonly SemaphoreSlim _concurrencyLimiter;
        private readonly TokenBucketRateLimiter _rateLimiter;
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new RequestLimiter with specified limits.
        /// </summary>
        /// <param name="maxConcurrentRequests">Maximum number of concurrent requests</param>
        /// <param name="requestsPerSecond">Maximum requests per second</param>
        /// <param name="logger">Logger for diagnostic information</param>
        public RequestLimiter(int maxConcurrentRequests, int requestsPerSecond, ILogger logger)
        {
            _concurrencyLimiter = new SemaphoreSlim(maxConcurrentRequests);
            _rateLimiter = new TokenBucketRateLimiter(
                new TokenBucketRateLimiterOptions
                {
                    TokenLimit = requestsPerSecond,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = int.MaxValue,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                    TokensPerPeriod = requestsPerSecond,
                    AutoReplenishment = true
                });
            _logger = logger;
        }

        /// <summary>
        /// Executes an action with rate limiting and concurrency limiting.
        /// </summary>
        /// <typeparam name="T">Return type of the action</typeparam>
        /// <param name="requestIdentifier">An identifier for logging purposes</param>
        /// <param name="action">The action to execute</param>
        /// <returns>The result of the action or default(T) if limits could not be respected</returns>
        public async Task<T?> ExecuteWithLimitsAsync<T>(string requestIdentifier, Func<Task<T>> action)
        {
            try
            {
                // Check rate limit first
                using var lease = await _rateLimiter.AcquireAsync(1);
                if (!lease.IsAcquired)
                {
                    _logger.LogWarning("Rate limit exceeded, request for {RequestIdentifier} was rejected", requestIdentifier);
                    return default;
                }

                // Then check concurrent requests limit
                await _concurrencyLimiter.WaitAsync();
                try
                {
                    return await action();
                }
                finally
                {
                    _concurrencyLimiter.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing request: {RequestIdentifier}", requestIdentifier);
                return default;
            }
        }
    }
}
