using System.Threading.RateLimiting;

namespace CdxEnrich.ClearlyDefined
{
    /// <summary>
    /// Manages rate limiting and concurrency limiting for HTTP requests.
    /// </summary>
    public class RequestLimiter
    {
        private readonly SemaphoreSlim _concurrencyLimiter;
        private readonly TokenBucketRateLimiter _rateLimiter;

        /// <summary>
        /// Creates a new RequestLimiter with specified limits.
        /// </summary>
        /// <param name="maxConcurrentRequests">Maximum number of concurrent requests</param>
        /// <param name="requestsPerSecond">Maximum requests per second</param>
        public RequestLimiter(int maxConcurrentRequests, int requestsPerSecond)
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
                    Log.Warn($"Rate limit exceeded, request for {requestIdentifier} was rejected");
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
                Log.Error($"Error executing request: {requestIdentifier} - {ex.Message}");
                return default;
            }
        }
    }
}
