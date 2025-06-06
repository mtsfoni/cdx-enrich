using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PackageUrl;

namespace CdxEnrich.ClearlyDefined
{
    public interface IClearlyDefinedClient
    {
        Task<List<string>?> GetClearlyDefinedLicensesAsync(PackageURL packageUrl, Provider provider);
    }

    public class ClearlyDefinedClient(HttpClient? httpClient = null, ILogger<ClearlyDefinedClient>? logger = null)
        : IClearlyDefinedClient
    {
        private const string ClearlyDefinedApiBase = "https://api.clearlydefined.io/definitions";
        private readonly HttpClient _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        private readonly ILogger<ClearlyDefinedClient> _logger = logger ?? NullLogger<ClearlyDefinedClient>.Instance;

        /// <summary>
        /// Ruft Lizenzinformationen für ein Paket von der ClearlyDefined API ab.
        /// </summary>
        /// <param name="packageUrl">Die PackageURL des Pakets</param>
        /// <returns>Eine Liste von Lizenzausdrücken oder null, wenn keine gefunden wurden</returns>
        public async Task<List<string>?> GetClearlyDefinedLicensesAsync(PackageURL packageUrl, Provider provider)
        {
            var apiUrl = CreateClearlyDefinedApiUrl(packageUrl, provider);

            const int maxRetries = 3;
            for (var retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    var response = await _httpClient.GetFromJsonAsync<ClearlyDefinedResponse>(apiUrl);
                    return response?.Licensed.Facets.Core.Discovered.Expressions;
                }
                catch (HttpRequestException ex)
                {
                    if (retry == maxRetries - 1)
                    {
                        _logger.LogError(ex, "Fehler bei API-Aufruf: {ApiUrl}", apiUrl);
                        throw;
                    }

                    _logger.LogWarning(ex, "HTTP error calling ClearlyDefined API (attempt {Attempt}/{MaxRetries}): {Message}", 
                        retry + 1, maxRetries, ex.Message);
                    await Task.Delay(1000 * (retry + 1)); // Exponential backoff
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing ClearlyDefined response for URL {ApiUrl}: {Message}", 
                        apiUrl, ex.Message);
                    throw;
                }
            }

            return null;
        }

        /// <summary>
        /// Erzeugt die API-URL für ClearlyDefined
        /// </summary>
        private string CreateClearlyDefinedApiUrl(PackageURL packageUrl, Provider provider)
        {
            // Fall 1: Namespace ist vorhanden
            if (packageUrl.Namespace != null)
            {
                return $"{ClearlyDefinedApiBase}/{packageUrl.Type}/{provider.Value}/{packageUrl.Namespace}/{packageUrl.Name}/{packageUrl.Version}?expand=-files";
            }
            // Fall 2: Kein Namespace vorhanden, "-" als Platzhalter verwenden
            else
            {
                return $"{ClearlyDefinedApiBase}/{packageUrl.Type}/{provider.Value}/-/{packageUrl.Name}/{packageUrl.Version}?expand=-files";
            }
        }
    }
}
