using System.Net;
using System.Net.Http.Json;
using PackageUrl;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace CdxEnrich.ClearlyDefined
{
    public interface IClearlyDefinedClient
    {
        Task<ClearlyDefinedResponse.LicensedData?> GetClearlyDefinedLicensedDataAsync(PackageURL packageUrl,
            Provider provider);
    }

    public class ClearlyDefinedClient : IClearlyDefinedClient
    {
        public static readonly Uri ClearlyDefinedApiBaseAddress = new ("https://api.clearlydefined.io");
        private readonly HttpClient _httpClient;
        private readonly ResiliencePipeline _resiliencePipeline;
        private readonly RequestLimiter _requestLimiter;
        private const int MaxRetryAttempts = 3;

        public ClearlyDefinedClient(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient(nameof(ClearlyDefinedClient));
            _resiliencePipeline = this.CreateResiliencePipeline();
            _requestLimiter =
                new RequestLimiter(maxConcurrentRequests: 10, requestsPerSecond: 33); // 33 per second = 1980 per minute
        }

        private ResiliencePipeline CreateResiliencePipeline()
        {
            // Configure the resilience pipeline with Polly v8
            var builder = new ResiliencePipelineBuilder();

            // Add retry strategy
            var retryDelay = TimeSpan.FromSeconds(1);
            builder.AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = MaxRetryAttempts,
                Delay = retryDelay,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true, // Random variation of wait time
                ShouldHandle = args =>
                {
                    // Handle HTTP errors
                    if (args.Outcome.Exception is HttpRequestException)
                    {
                        Log.Warn($"HTTP request error detected. Will retry after delay: {retryDelay}");
                        return ValueTask.FromResult(true);
                    }

                    // Handle rate limit errors if the result is an HttpResponseMessage
                    if (args.Outcome.Result is HttpResponseMessage { StatusCode: HttpStatusCode.TooManyRequests })
                    {
                        Log.Warn($"Rate limit error detected. Will retry after delay: {retryDelay}");
                        return ValueTask.FromResult(true);
                    }
                    
                    // Handle timeouts
                    if (args.Outcome.Exception is TimeoutRejectedException)
                    {
                        Log.Warn($"Timeout detected. Will retry after delay: {retryDelay}");
                        return ValueTask.FromResult(true);
                    }

                    return ValueTask.FromResult(false);
                },
                OnRetry = args =>
                {
                    if (args.Outcome.Result is HttpResponseMessage response &&
                        response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        Log.Warn($"Rate limit reached on ClearlyDefined API call. Retry attempt {args.AttemptNumber}/{MaxRetryAttempts} after {args.RetryDelay}");

                        // Extract rate limit information from headers if available
                        if (response.Headers.TryGetValues("x-ratelimit-remaining", out var remaining) &&
                            response.Headers.TryGetValues("x-ratelimit-limit", out var limit))
                        {
                            Log.Info($"Rate Limit Info: {string.Join(",", remaining)}/{string.Join(",", limit)} remaining");
                        }
                    }
                    else
                    {
                        Log.Warn($"HTTP error on ClearlyDefined API call. Retry attempt {args.AttemptNumber}/{MaxRetryAttempts} after {args.RetryDelay}");
                    }

                    return ValueTask.CompletedTask;
                }
            });

            // Add timeout strategy
            builder.AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(60),
                OnTimeout = args =>
                {
                    var seconds = args.Timeout.ToString("ss");
                    Log.Warn($"Timeout on ClearlyDefined API call after {seconds} seconds");
                    return ValueTask.CompletedTask;
                }
            });

            return builder.Build();
        }

        /// <summary>
        /// Retrieves license data for a package from the ClearlyDefined API.
        /// </summary>
        /// <param name="packageUrl">The PackageURL of the package</param>
        /// <param name="provider">The provider to use</param>
        /// <returns>Licensed data or null if not found</returns>
        public async Task<ClearlyDefinedResponse.LicensedData?> GetClearlyDefinedLicensedDataAsync(
            PackageURL packageUrl, Provider provider)
        {
            var requestPath = CreateRequestPath(packageUrl, provider);

            return await _requestLimiter.ExecuteWithLimitsAsync(
                requestPath, async () =>
                {
                    var response = await _resiliencePipeline.ExecuteAsync<HttpResponseMessage>(
                        async cancellationToken => await _httpClient.GetAsync(requestPath, cancellationToken),
                        CancellationToken.None);

                    if (!response.IsSuccessStatusCode)
                    {
                        Log.Error($"API call unsuccessful after retry {MaxRetryAttempts} attempts: {response.StatusCode} for {requestPath}");
                        return null;
                    }

                    var data = await response.Content.ReadFromJsonAsync<ClearlyDefinedResponse>();
                    Log.Info($"Successfully retrieved data from ClearlyDefined API for package: {packageUrl}");
                    return data?.Licensed;
                });
        }

        /// <summary>
        /// Creates the request path for the ClearlyDefined API based on the PackageURL and provider.
        /// </summary>
        private static string CreateRequestPath(PackageURL packageUrl, Provider provider)
        {
            return $"definitions/{packageUrl.Type}/{provider.Value}/{packageUrl.Namespace ?? "-"}/{packageUrl.Name}/{packageUrl.Version}?expand=-files";
        }
    }
}