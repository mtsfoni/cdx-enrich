using CdxEnrich;
using CdxEnrich.Config;
using CdxEnrich.FunctionalHelpers;
using CdxEnrich.PackageUrl;
using CycloneDX.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CdxEnrich.Actions
{
    public static class ReplaceLicenseByClearlyDefined
    {
        static readonly string moduleName = nameof(ReplaceLicenseByClearlyDefined);
        private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(60) };
        private const string ClearlyDefinedApiBase = "https://api.clearlydefined.io/definitions";

        public static Component? GetComponentByBomRef(Bom bom, string bomRef)
        {
            return bom.Components?.Find(comp => comp.BomRef == bomRef);
        }

        private static Result<ConfigRoot> RefMustNotBeNullOrEmpty(ConfigRoot config)
        {
            if (config.ReplaceLicenseByClearlyDefined?.Exists(rec => string.IsNullOrEmpty(rec.Ref)) == true)
            {
                return InvalidConfigError.Create<ConfigRoot>(moduleName, "Ref must be set and cannot be an empty string.");
            }
            else
            {
                return new Ok<ConfigRoot>(config);
            }
        }

        private static Result<ConfigRoot> RefMustBeValidPurl(ConfigRoot config)
        {
            if (config.ReplaceLicenseByClearlyDefined == null)
                return new Ok<ConfigRoot>(config);

            foreach (var item in config.ReplaceLicenseByClearlyDefined)
            {
                if (item.Ref != null && !PackageUrl.PackageUrl.TryParse(item.Ref, out _))
                {
                    return InvalidConfigError.Create<ConfigRoot>(moduleName, $"Invalid PURL format: {item.Ref}");
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
                Console.Error.WriteLine($"Error processing component {purl}: {ex.Message}");
            }
        }

        private static async Task<List<string>> GetClearlyDefinedLicensesAsync(string purlString)
        {
            var packageUrl = PackageUrl.PackageUrl.Parse(purlString);
            var apiUrl = packageUrl.ToClearlyDefinedApiUrl(ClearlyDefinedApiBase);

            const int maxRetries = 3;
            for (int retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    var response = await HttpClient.GetFromJsonAsync<ClearlyDefinedResponse>(apiUrl);
                    
                    if (response?.Licensed?.Facets?.Core?.Discovered?.Expressions != null)
                    {
                        return response.Licensed.Facets.Core.Discovered.Expressions;
                    }
                    
                    return null;
                }
                catch (HttpRequestException ex)
                {
                    if (retry == maxRetries - 1)
                    {
                        Console.Error.WriteLine($"Fehler bei API-Aufruf: {apiUrl}");
                        throw;
                    }
                    
                    Console.Error.WriteLine($"HTTP error calling ClearlyDefined API (attempt {retry+1}/{maxRetries}): {ex.Message}");
                    await Task.Delay(1000 * (retry + 1)); // Exponential backoff
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error parsing ClearlyDefined response for URL {apiUrl}: {ex.Message}");
                    throw;
                }
            }

            return null;
        }


        private class ClearlyDefinedResponse
        {
            [JsonPropertyName("licensed")]
            public LicensedData Licensed { get; set; }

            public class LicensedData
            {
                [JsonPropertyName("facets")]
                public Facets Facets { get; set; }
            }

            public class Facets
            {
                [JsonPropertyName("core")]
                public Core Core { get; set; }
            }

            public class Core
            {
                [JsonPropertyName("discovered")]
                public Discovered Discovered { get; set; }
            }

            public class Discovered
            {
                [JsonPropertyName("expressions")]
                public List<string> Expressions { get; set; }
            }
        }
    }
}
