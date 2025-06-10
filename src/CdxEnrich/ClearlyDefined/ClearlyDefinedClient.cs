using System.Net;
using System.Net.Http.Json;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PackageUrl;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace CdxEnrich.ClearlyDefined
{
    public interface IClearlyDefinedClient
    {
        Task<List<string>?> GetClearlyDefinedLicensesAsync(PackageURL packageUrl, Provider provider);
    }

    public class ClearlyDefinedClient : IClearlyDefinedClient
    {
        private const string ClearlyDefinedApiBase = "https://api.clearlydefined.io/definitions";
        private readonly HttpClient _httpClient;
        private readonly ILogger<ClearlyDefinedClient> _logger;
        private readonly ResiliencePipeline _resiliencePipeline;
        
        // Token Bucket Rate Limiter für max. 250 Anfragen pro Minute
        private static readonly TokenBucketRateLimiter _rateLimiter = new TokenBucketRateLimiter(
            new TokenBucketRateLimiterOptions
            {
                TokenLimit = 250,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 100,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                TokensPerPeriod = 250,
                AutoReplenishment = true
            });

        public ClearlyDefinedClient(HttpClient? httpClient = null, ILogger<ClearlyDefinedClient>? logger = null)
        {
            _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            _logger = logger ?? NullLogger<ClearlyDefinedClient>.Instance;
            _resiliencePipeline = this.CreateResiliencePipeline();
        }

        private ResiliencePipeline CreateResiliencePipeline()
        {
            // Configure resilience pipeline with Polly v8
            var builder = new ResiliencePipelineBuilder();
            
            // Add retry strategy
            builder.AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true, // Random variation of wait time
                ShouldHandle = args =>
                {
                    // Handle HTTP errors
                    if (args.Outcome.Exception is HttpRequestException)
                        return ValueTask.FromResult(true);
                    
                    // Handle rate limit errors if the result is an HttpResponseMessage
                    if (args.Outcome.Result is HttpResponseMessage response && 
                        response.StatusCode == HttpStatusCode.TooManyRequests)
                        return ValueTask.FromResult(true);
                    
                    return ValueTask.FromResult(false);
                },
                OnRetry = args =>
                {
                    if (args.Outcome.Result is HttpResponseMessage response && 
                        response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        this._logger.LogWarning(
                            "Rate limit reached on ClearlyDefined API call. Retry attempt {Attempt} after {Delay}",
                            args.AttemptNumber, args.RetryDelay);
                        
                        // Extract rate limit information from headers if available
                        if (response.Headers.TryGetValues("x-ratelimit-remaining", out var remaining) &&
                            response.Headers.TryGetValues("x-ratelimit-limit", out var limit))
                        {
                            this._logger.LogInformation("Rate Limit Info: {Remaining}/{Limit} remaining", 
                                string.Join(",", remaining), string.Join(",", limit));
                        }
                    }
                    else
                    {
                        this._logger.LogWarning(
                            "HTTP error on ClearlyDefined API call. Retry attempt {Attempt} after {Delay}",
                            args.AttemptNumber, args.RetryDelay);
                    }
                    
                    return ValueTask.CompletedTask;
                }
            });
            
            // Add timeout strategy
            builder.AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(30),
                OnTimeout = args =>
                {
                    this._logger.LogWarning("Timeout on ClearlyDefined API call after 30 seconds");
                    return ValueTask.CompletedTask;
                }
            });
            
            return builder.Build();
        }

        /// <summary>
        /// Retrieves license information for a package from the ClearlyDefined API.
        /// </summary>
        /// <param name="packageUrl">The PackageURL of the package</param>
        /// <param name="provider">The provider to use</param>
        /// <returns>A list of license expressions or null if none were found</returns>
        public async Task<List<string>?> GetClearlyDefinedLicensesAsync(PackageURL packageUrl, Provider provider)
        {
            var apiUrl = CreateClearlyDefinedApiUrl(packageUrl, provider);

            try
            {
                // Acquire permission from the rate limiter
                using var lease = await _rateLimiter.AcquireAsync(1);
                
                if (lease.IsAcquired)
                {
                    // Execute resilience pipeline
                    var response = await _resiliencePipeline.ExecuteAsync<HttpResponseMessage>(
                        async cancellationToken => await _httpClient.GetAsync(apiUrl, cancellationToken),
                        CancellationToken.None);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var data = await response.Content.ReadFromJsonAsync<ClearlyDefinedResponse>();
                        return data?.Licensed.Facets.Core.Discovered.Expressions;
                    }
                    else
                    {
                        // Still not successful after retries
                        _logger.LogError("API call unsuccessful after retry attempts: {StatusCode} for {ApiUrl}", 
                            response.StatusCode, apiUrl);
                        return null;
                    }
                }
                else
                {
                    _logger.LogWarning("Rate limit exceeded, request for {ApiUrl} was rejected", apiUrl);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during API call: {ApiUrl}", apiUrl);
                return null;
            }
        }

        /// <summary>
        /// Creates the API URL for ClearlyDefined
        /// </summary>
        private string CreateClearlyDefinedApiUrl(PackageURL packageUrl, Provider provider)
        {
            // Case 1: Namespace is present
            if (packageUrl.Namespace != null)
            {
                return $"{ClearlyDefinedApiBase}/{packageUrl.Type}/{provider.Value}/{packageUrl.Namespace}/{packageUrl.Name}/{packageUrl.Version}?expand=-files";
            }
            // Case 2: No namespace present, use "-" as placeholder
            else
            {
                return $"{ClearlyDefinedApiBase}/{packageUrl.Type}/{provider.Value}/-/{packageUrl.Name}/{packageUrl.Version}?expand=-files";
            }
        }
    }
}