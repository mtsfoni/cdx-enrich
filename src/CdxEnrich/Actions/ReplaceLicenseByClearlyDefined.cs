using CdxEnrich.ClearlyDefined;
using CdxEnrich.Config;
using CdxEnrich.FunctionalHelpers;
using CdxEnrich.Logging;
using CycloneDX.Models;
using Microsoft.Extensions.Logging;
using PackageUrl;

namespace CdxEnrich.Actions
{
    public static class ReplaceLicenseByClearlyDefined
    {
        private static readonly ILogger Logger = new ConsoleLogger(nameof(ReplaceLicenseByClearlyDefined));
        private static readonly string ModuleName = nameof(ReplaceLicenseByClearlyDefined);

        private static readonly IClearlyDefinedClient ClearlyDefinedClient = new ClearlyDefinedClient(
            logger: new ConsoleLogger<ClearlyDefinedClient>());

        private static readonly LicenseResolver LicenseResolver =
            new (new ConsoleLogger<LicenseResolver>());

        private static readonly IList<PackageType> NotSupportedPackageTypes = new List<PackageType>
        {
            PackageType.Composer,
            PackageType.Conda,
            PackageType.Condasrc,
            PackageType.Deb,
            PackageType.Debsrc,
            PackageType.Git,
            PackageType.Go,
            PackageType.SourceArchive,
        };

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

        private static bool IsSupported(PackageType packageType)
        {
            return !NotSupportedPackageTypes.Contains(packageType);
        }


        public static Result<ConfigRoot> CheckConfig(ConfigRoot config)
        {
            return RefMustNotBeNullOrEmpty(config);
        }

        public static InputTuple Execute(InputTuple inputs)
        {
            var tasks = new List<Task>();

            var configEntries = inputs.Config.ReplaceLicenseByClearlyDefined?
                .Where(item => item.Ref != null)
                .ToList();
            
            if(configEntries == null)
            {
                return inputs;
            }

            foreach (var configEntryRef in configEntries.Select(x => x.Ref))
            {
                var component = GetComponentByBomRef(inputs.Bom, configEntryRef!);
                if (component == null)
                {
                    Logger.LogInformation("Component with bom ref '{BomRef}' not found in the BOM.", configEntryRef);
                    continue;
                }
                
                if(string.IsNullOrEmpty(component.Purl))
                {
                    Logger.LogInformation("Component with bom ref '{BomRef}' does not have a PURL set.", configEntryRef);
                    continue;
                }

                if (!TryParsePurl(component.Purl, out var packageUrl))
                {
                    Logger.LogError("Invalid PURL format: '{PackageUrl}'", component.Purl);
                    continue;
                }

                PackageType? packageType = null;
                if (packageUrl != null && !PackageType.TryFromPurlType(packageUrl.Type, out packageType))
                {
                    Logger.LogError("Package type '{PackageUrlType}' is not supported by ClearlyDefined. Supported types are: {SupportedPackageTypes}", packageUrl.Type, string.Join(", ", PackageType.All.Where(IsSupported).Select(pt => pt.Value)));
                    continue;
                }

                if (packageType != null && !IsSupported(packageType))
                {
                    Logger.LogError("Package type '{PackageTypeName}' is currently not supported by CdxEnrich. Supported types are: {SupportedPackageTypes}", packageType.Name, string.Join(", ", PackageType.All.Where(IsSupported).Select(pt => pt.Value)));
                    continue;
                }
                
                var provider = packageType!.DefaultProvider;
                tasks.Add(ProcessComponentAsync(component, packageUrl!, provider));
            }

            try
            {
                Task.WhenAll(tasks).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing components {Message}", ex.Message);
            }

            return inputs;
        }

        private static async Task ProcessComponentAsync(Component component, PackageURL packageUrl, Provider provider)
        {
            try
            {
                // Fetching license data from ClearlyDefined
                var licensedData = await ClearlyDefinedClient.GetClearlyDefinedLicensedDataAsync(packageUrl, provider);
            
                if (licensedData == null || licensedData.Declared == null || licensedData.Facets == null)
                {
                    Logger.LogInformation("No license data found for package: {PackageUrl}", packageUrl);
                    return;
                }
            
                // Using the resolver to determine the LicenseChoice
                var licenseChoice = LicenseResolver.Resolve(packageUrl, licensedData);

                if (licenseChoice == null)
                {
                    return;
                }

                component.Licenses = [licenseChoice];
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing component {PackageUrl}: {Message}", packageUrl, ex.Message);
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