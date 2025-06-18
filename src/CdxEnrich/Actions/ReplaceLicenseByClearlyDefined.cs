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
            new LicenseResolver(new ConsoleLogger<LicenseResolver>());

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

        private static Result<ConfigRoot> RefMustBeSupportedPackageType(ConfigRoot config)
        {
            if (config.ReplaceLicenseByClearlyDefined == null)
            {
                return new Ok<ConfigRoot>(config);
            }

            foreach (var itemRef in config.ReplaceLicenseByClearlyDefined.Select(item => item.Ref))
            {
                PackageURL? purl = null;
                if (itemRef != null && !TryParsePurl(itemRef, out purl))
                {
                    return InvalidConfigError.Create<ConfigRoot>(ModuleName, $"Invalid PURL format: {itemRef}");
                }

                PackageType? packageType = null;
                if (purl != null && !PackageType.TryFromPurlType(purl.Type, out packageType))
                {
                    return InvalidConfigError.Create<ConfigRoot>(ModuleName,
                        $"Package type '{purl.Type}' is not supported by ClearlyDefined. Supported types are: {string.Join(", ", PackageType.All.Select(pt => pt.Value))}");
                }

                if (packageType != null && !IsSupported(packageType))
                {
                    return InvalidConfigError.Create<ConfigRoot>(ModuleName,
                        $"Package type '{packageType.Name}' is currently not supported by CdxEnrich. Supported types are: {string.Join(", ", PackageType.All.Where(IsSupported).Select(pt => pt.Value))}");
                }
            }

            return new Ok<ConfigRoot>(config);
        }

        private static bool IsSupported(PackageType packageType)
        {
            return !NotSupportedPackageTypes.Contains(packageType);
        }


        public static Result<ConfigRoot> CheckConfig(ConfigRoot config)
        {
            return RefMustNotBeNullOrEmpty(config)
                .Bind(RefMustBeSupportedPackageType);
        }

        public static InputTuple Execute(InputTuple inputs)
        {
            var tasks = new List<Task>();

            var configEntries = inputs.Config.ReplaceLicenseByClearlyDefined?
                .Where(item => item.Ref != null)
                .ToList();
            
            if(configEntries == null)
            {
                Logger.LogInformation("No valid configuration entries found for ClearlyDefined replacement.");
                return inputs;
            }

            foreach (var configEntry in configEntries)
            {
                var component = GetComponentByBomRef(inputs.Bom, configEntry.Ref!);
                if (component == null)
                {
                    continue;
                }

                var packageUrl = new PackageURL(configEntry.Ref!);
                var packageType = PackageType.FromPurlType(packageUrl.Type);
                var provider = packageType.DefaultProvider;
                tasks.Add(ProcessComponentAsync(component, packageUrl, provider));
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
                // Holen der Lizenzdaten von ClearlyDefined
                var licensedData = await ClearlyDefinedClient.GetClearlyDefinedLicensedDataAsync(packageUrl, provider);

                if (licensedData == null)
                {
                    Logger.LogInformation("No license data found for package: {PackageUrl}", packageUrl);
                    return;
                }

                // Verwenden des Resolvers zum Ermitteln der LicenseChoice
                var licenseChoice = LicenseResolver.Resolve(packageUrl, licensedData);

                if (licenseChoice == null)
                {
                    Logger.LogInformation("No applicable license choices resolved for package: {PackageUrl}",
                        packageUrl);
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