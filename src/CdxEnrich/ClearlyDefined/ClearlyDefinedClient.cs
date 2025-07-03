using System.Net;
using System.Net.Http.Json;
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
        Task<ClearlyDefinedResponse.LicensedData?> GetClearlyDefinedLicensedDataAsync(PackageURL packageUrl,
            Provider provider);
    }

    public class ClearlyDefinedClient : IClearlyDefinedClient
    {
        private const string ClearlyDefinedApiBase = "https://api.clearlydefined.io/definitions";
        private readonly HttpClient _httpClient;
        private readonly ILogger<ClearlyDefinedClient> _logger;
        private readonly ResiliencePipeline _resiliencePipeline;
        private readonly RequestLimiter _requestLimiter;
        private const int MaxRetryAttempts = 3;

        public ClearlyDefinedClient(HttpClient? httpClient = null, ILogger<ClearlyDefinedClient>? logger = null)
        {
            _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            _logger = logger ?? NullLogger<ClearlyDefinedClient>.Instance;
            _resiliencePipeline = this.CreateResiliencePipeline();
            _requestLimiter =
                new RequestLimiter(maxConcurrentRequests: 10, requestsPerSecond: 33,
                    _logger); // 33 per second = 1980 per minute
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
                        this._logger.LogWarning(
                            "HTTP request error detected. Will retry after delay: {Delay}",
                            retryDelay);
                        return ValueTask.FromResult(true);
                    }

                    // Handle rate limit errors if the result is an HttpResponseMessage
                    if (args.Outcome.Result is HttpResponseMessage { StatusCode: HttpStatusCode.TooManyRequests })
                    {
                        _logger.LogWarning(
                            "Rate limit error detected. Will retry after delay: {Delay}",
                            retryDelay);
                        return ValueTask.FromResult(true);
                    }

                    return ValueTask.FromResult(false);
                },
                OnRetry = args =>
                {
                    if (args.Outcome.Result is HttpResponseMessage response &&
                        response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        this._logger.LogWarning(
                            "Rate limit reached on ClearlyDefined API call. Retry attempt {Attempt}/{MaxAttempts} after {Delay}",
                            args.AttemptNumber, MaxRetryAttempts, args.RetryDelay);

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
                            "HTTP error on ClearlyDefined API call. Retry attempt {Attempt}/{MaxAttempts} after {Delay}",
                            args.AttemptNumber, MaxRetryAttempts, args.RetryDelay);
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
                    this._logger.LogWarning("Timeout on ClearlyDefined API call after {Seconds} seconds", seconds);
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
            var apiUrl = CreateClearlyDefinedApiUrl(packageUrl, provider);

            return await _requestLimiter.ExecuteWithLimitsAsync(
                apiUrl, async () =>
                {
                    var response = await _resiliencePipeline.ExecuteAsync<HttpResponseMessage>(
                        async cancellationToken => await _httpClient.GetAsync(apiUrl, cancellationToken),
                        CancellationToken.None);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("API call unsuccessful after retry {RetryAttempts} attempts: {StatusCode} for {ApiUrl}",
                            MaxRetryAttempts, response.StatusCode, apiUrl);
                        return null;
                    }

                    var data = await response.Content.ReadFromJsonAsync<ClearlyDefinedResponse>();
                    _logger.LogInformation(
                        "Successfully retrieved data from ClearlyDefined API for package: {PackageUrl}",
                        packageUrl);
                    return data?.Licensed;
                });
        }

        /// <summary>
        /// Creates the API URL for ClearlyDefined
        /// </summary>
        private string CreateClearlyDefinedApiUrl(PackageURL packageUrl, Provider provider)
        {
            // Case 1: Namespace is present
            if (packageUrl.Namespace != null)
            {
                return
                    $"{ClearlyDefinedApiBase}/{packageUrl.Type}/{provider.Value}/{packageUrl.Namespace}/{packageUrl.Name}/{packageUrl.Version}?expand=-files";
            }
            // Case 2: No namespace present, use "-" as placeholder
            else
            {
                return
                    $"{ClearlyDefinedApiBase}/{packageUrl.Type}/{provider.Value}/-/{packageUrl.Name}/{packageUrl.Version}?expand=-files";
            }
        }
    }
}