using System.Net.Http.Json;
using System.Text.Json.Serialization;
using PackageUrl;

namespace CdxEnrich.ClearlyDefined
{
    public interface IClearlyDefinedClient
    {
        Task<List<string>?> GetClearlyDefinedLicensesAsync(PackageURL packageUrl);
    }

    public class ClearlyDefinedClient : IClearlyDefinedClient
    {
        private const string ClearlyDefinedApiBase = "https://api.clearlydefined.io/definitions";
        private readonly HttpClient _httpClient;

        public ClearlyDefinedClient(HttpClient? httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        }

        /// <summary>
        /// Ruft Lizenzinformationen für ein Paket von der ClearlyDefined API ab.
        /// </summary>
        /// <param name="packageUrl">Die PackageURL des Pakets</param>
        /// <returns>Eine Liste von Lizenzausdrücken oder null, wenn keine gefunden wurden</returns>
        public async Task<List<string>?> GetClearlyDefinedLicensesAsync(PackageURL packageUrl)
        {
            var apiUrl = CreateClearlyDefinedApiUrl(packageUrl);

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
                        await Console.Error.WriteLineAsync($"Fehler bei API-Aufruf: {apiUrl}");
                        throw;
                    }

                    await Console.Error.WriteLineAsync(
                        $"HTTP error calling ClearlyDefined API (attempt {retry + 1}/{maxRetries}): {ex.Message}");
                    await Task.Delay(1000 * (retry + 1)); // Exponential backoff
                }
                catch (Exception ex)
                {
                    await Console.Error.WriteLineAsync($"Error parsing ClearlyDefined response for URL {apiUrl}: {ex.Message}");
                    throw;
                }
            }

            return null;
        }

        /// <summary>
        /// Erzeugt die API-URL für ClearlyDefined
        /// </summary>
        private string CreateClearlyDefinedApiUrl(PackageURL packageUrl)
        {
            // Ermittle den passenden Provider für den PURL-Typ
            var provider = Provider.FromPurlType(packageUrl.Type);
            
            // Fall 1: Namespace ist vorhanden
            if (packageUrl.Namespace != null)
            {
                return $"{ClearlyDefinedApiBase}/{packageUrl.Type}/{provider.ApiString}/{packageUrl.Namespace}/{packageUrl.Name}/{packageUrl.Version}?expand=-files";
            }
            // Fall 2: Kein Namespace vorhanden, "-" als Platzhalter verwenden
            else
            {
                return $"{ClearlyDefinedApiBase}/{packageUrl.Type}/{provider.ApiString}/-/{packageUrl.Name}/{packageUrl.Version}?expand=-files";
            }
        }
    }

    public class ClearlyDefinedResponse
    {
        [JsonPropertyName("licensed")] public required LicensedData Licensed { get; init; }

        public class LicensedData
        {
            [JsonPropertyName("facets")] public required Facets Facets { get; init; }
        }

        public class Facets
        {
            [JsonPropertyName("core")] public required Core Core { get; init; }
        }

        public class Core
        {
            [JsonPropertyName("discovered")] public required Discovered Discovered { get; init; }
        }

        public class Discovered
        {
            [JsonPropertyName("expressions")] public List<string>? Expressions { get; init; }
        }
    }
}
