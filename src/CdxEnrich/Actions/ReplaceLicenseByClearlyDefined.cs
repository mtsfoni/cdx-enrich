using CdxEnrich.ClearlyDefined;
using CdxEnrich.Config;
using CdxEnrich.FunctionalHelpers;
using CycloneDX.Models;
using PackageUrl;

namespace CdxEnrich.Actions
{
    public static class ReplaceLicenseByClearlyDefined
    {
        private static readonly string ModuleName = nameof(ReplaceLicenseByClearlyDefined);
        private static readonly IClearlyDefinedClient ClearlyDefinedClient = new ClearlyDefinedClient();

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
                    var packageUrl = new PackageURL(item.Ref!);
                    var packageType = PackageType.FromPurlType(packageUrl.Type);
                    var provider = packageType.DefaultProvider;
                    if (component != null)
                    {
                        tasks.Add(ProcessComponentAsync(component, packageUrl, provider));
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

        private static async Task ProcessComponentAsync(Component component, PackageURL packageUrl, Provider provider)
        {
            try
            {
                var cdLicenses = await ClearlyDefinedClient.GetClearlyDefinedLicensesAsync(packageUrl, provider);

                if (cdLicenses == null || !cdLicenses.Any())
                {
                    Console.WriteLine($"No ClearlyDefined licenses found for {packageUrl}");
                    return;
                }

                component.Licenses = cdLicenses.Select(expression => new LicenseChoice
                {
                    License = new License { Id = expression }
                }).ToList();
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error processing component {packageUrl}: {ex.Message}");
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