using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CdxEnrich.ClearlyDefined;
using CdxEnrich.Config;
using CdxEnrich.FunctionalHelpers;
using CycloneDX.Models;
using PackageUrl;

namespace CdxEnrich.Actions
{
    public static class ReplaceLicenseByClearlyDefined
    {
        private const string ClearlyDefinedApiBase = "https://api.clearlydefined.io/definitions";
        private static readonly string ModuleName = nameof(ReplaceLicenseByClearlyDefined);
        private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(60) };

        private static Component? GetComponentByBomRef(Bom bom, string bomRef)
        {
            return bom.Components?.Find(comp => comp.BomRef == bomRef);
        }

        private static Result<ConfigRoot> RefMustNotBeNullOrEmpty(ConfigRoot config)
        {
            if (config.ReplaceLicenseByClearlyDefined?.Exists(rec => string.IsNullOrEmpty(rec.Ref)) == true)
            {
                return InvalidConfigError.Create<ConfigRoot>(ModuleName,
                    "Ref must be set and cannot be an empty string.");
            }

            return new Ok<ConfigRoot>(config);
        }

        private static Result<ConfigRoot> RefMustBeValidPurl(ConfigRoot config)
        {
            if (config.ReplaceLicenseByClearlyDefined == null)
            {
                return new Ok<ConfigRoot>(config);
            }

            foreach (var item in config.ReplaceLicenseByClearlyDefined)
            {
                if (item.Ref != null && !TryParsePurl(item.Ref, out _))
                {
                    return InvalidConfigError.Create<ConfigRoot>(ModuleName, $"Invalid PURL format: {item.Ref}");
                }
            }

            return new Ok<ConfigRoot>(config);
        }

        public static Result<ConfigRoot> CheckConfig(ConfigRoot config)
        {
            return RefMustNotBeNullOrEmpty(config)
                .Bind(RefMustBeValidPurl);
        }

        public static InputTuple Execute(InputTuple inputs)
        {
            var tasks = new List<Task>();

            inputs.Config.ReplaceLicenseByClearlyDefined?
                .Where(item => item.Ref != null)
                .ToList()
                .ForEach(item =>
                {
                    var component = GetComponentByBomRef(inputs.Bom, item.Ref!);
                    if (component != null)
                    {
                        tasks.Add(ProcessComponentAsync(component, item.Ref!));
                    }
                });

            try
            {
                Task.WhenAll(tasks).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error processing components: {ex.Message}");
            }

            return inputs;
        }

        private static async Task ProcessComponentAsync(Component component, string purl)
        {
            try
            {
                var cdLicenses = await GetClearlyDefinedLicensesAsync(purl);

                if (cdLicenses == null || !cdLicenses.Any())
                {
                    Console.WriteLine($"No ClearlyDefined licenses found for {purl}");
                    return;
                }

                component.Licenses = cdLicenses.Select(expression => new LicenseChoice
                {
                    License = new License { Id = expression }
                }).ToList();
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error processing component {purl}: {ex.Message}");
            }
        }

        private static async Task<List<string>?> GetClearlyDefinedLicensesAsync(string purlString)
        {
            var packageUrl = new PackageURL(purlString);
            var apiUrl = CreateClearlyDefinedApiUrl(packageUrl, ClearlyDefinedApiBase);

            const int maxRetries = 3;
            for (var retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    var response = await HttpClient.GetFromJsonAsync<ClearlyDefinedResponse>(apiUrl);

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
        private static string CreateClearlyDefinedApiUrl(PackageURL packageUrl, string apiBase)
        {
            // Ermittle den passenden Provider für den PURL-Typ
            var provider = Provider.FromPurlType(packageUrl.Type);
            
            // Fall 1: Namespace ist vorhanden
            if (packageUrl.Namespace != null)
            {
                return $"{apiBase}/{packageUrl.Type}/{provider.ApiString}/{packageUrl.Namespace}/{packageUrl.Name}/{packageUrl.Version}?expand=-files";
            }
            // Fall 2: Kein Namespace vorhanden, "-" als Platzhalter verwenden
            else
            {
                return $"{apiBase}/{packageUrl.Type}/{provider.ApiString}/-/{packageUrl.Name}/{packageUrl.Version}?expand=-files";
            }
        }

        private sealed class ClearlyDefinedResponse
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

        private static bool TryParsePurl(string purlString, out PackageURL? packageUrl)
        {
            packageUrl = null;
            
            try
            {
                packageUrl = new PackageURL(purlString);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}