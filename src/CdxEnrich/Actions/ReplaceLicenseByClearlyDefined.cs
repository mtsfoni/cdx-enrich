using CdxEnrich.ClearlyDefined;
using CdxEnrich.Config;
using CdxEnrich.FunctionalHelpers;
using CycloneDX.Models;
using Microsoft.Extensions.Logging;
using PackageUrl;

namespace CdxEnrich.Actions
{
    public class ReplaceLicenseByClearlyDefined
    {
        private readonly ILogger<ReplaceLicenseByClearlyDefined> _logger;
        private readonly IClearlyDefinedClient _clearlyDefinedClient;
        private readonly ILicenseResolver _licenseResolver;
        
        private static readonly string ModuleName = nameof(ReplaceLicenseByClearlyDefined);

        public ReplaceLicenseByClearlyDefined(
            ILogger<ReplaceLicenseByClearlyDefined> logger,
            IClearlyDefinedClient clearlyDefinedClient,
            ILicenseResolver licenseResolver)
        {
            _logger = logger;
            _clearlyDefinedClient = clearlyDefinedClient;
            _licenseResolver = licenseResolver;
        }

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
        
        private static Result<ConfigRoot> RefsMustBeUnique(ConfigRoot config)
        {
            var duplicateRefs = config.ReplaceLicenseByClearlyDefined?.GroupBy(x => x.Ref)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            
            if (duplicateRefs?.Count > 0)
            {
                return InvalidConfigError.Create<ConfigRoot>(ModuleName,
                    "The Refs must be unique. Found duplicates: " + string.Join(", ", duplicateRefs));
            }

            return new Ok<ConfigRoot>(config);
        }

        private static bool IsSupported(PackageType packageType)
        {
            return !NotSupportedPackageTypes.Contains(packageType);
        }


        public Result<ConfigRoot> CheckConfig(ConfigRoot config)
        {
            return RefMustNotBeNullOrEmpty(config)
                .Bind(RefsMustBeUnique);
        }
        
        public Result<InputTuple> CheckBomAndConfigCombination(InputTuple inputs)
        {
            var configEntries = inputs.Config.ReplaceLicenseByClearlyDefined?
                .Where(item => item.Ref != null)
                .ToList();
            
            if(configEntries == null)
            {
                return new Ok<InputTuple>(inputs);
            }

            foreach (var configEntryRef in configEntries.Select(x => x.Ref))
            {
                var component = GetComponentByBomRef(inputs.Bom, configEntryRef!);
                if (component == null)
                {
                    return InvalidBomAndConfigCombinationError.Create<InputTuple>(
                        $"Component with bom ref '{configEntryRef}' not found in the BOM.");
                }

                if (string.IsNullOrEmpty(component.Purl))
                {
                    return InvalidBomAndConfigCombinationError.Create<InputTuple>($"Component with bom ref '{configEntryRef}' does not have a PURL set.");
                }

                if (!TryParsePurl(component.Purl, out var packageUrl))
                {
                    return InvalidBomAndConfigCombinationError.Create<InputTuple>($"Invalid PURL format: '{component.Purl}'");
                }

                PackageType? packageType = null;
                if (packageUrl != null && !PackageType.TryFromPurlType(packageUrl.Type, out packageType))
                {
                    return InvalidBomAndConfigCombinationError.Create<InputTuple>(
                        $"Package type '{packageUrl.Type}' is not supported by ClearlyDefined. Supported types are: {string.Join(", ", PackageType.All.Where(IsSupported).Select(pt => pt.Value))}");
                }

                if (packageType != null && !IsSupported(packageType))
                {
                    return InvalidBomAndConfigCombinationError.Create<InputTuple>(
                        $"Package type '{packageType.Name}' is currently not supported by CdxEnrich. Supported types are: {string.Join(", ", PackageType.All.Where(IsSupported).Select(pt => pt.Value))}");
                }
            }
            return new Ok<InputTuple>(inputs);
        }

        public InputTuple Execute(InputTuple inputs)
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
                var packageUrl = new PackageURL(component!.Purl);
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
                _logger.LogError(ex, "Error processing components {Message}", ex.Message);
            }

            return inputs;
        }

        private async Task ProcessComponentAsync(Component component, PackageURL packageUrl, Provider provider)
        {
            try
            {
                // Fetching license data from ClearlyDefined
                var licensedData = await _clearlyDefinedClient.GetClearlyDefinedLicensedDataAsync(packageUrl, provider);
            
                if (licensedData == null || licensedData.Declared == null || licensedData.Facets == null)
                {
                    _logger.LogInformation("No license data found for package: {PackageUrl}", packageUrl);
                    return;
                }
            
                // Using the resolver to determine the LicenseChoice
                var licenseChoice = _licenseResolver.Resolve(packageUrl, licensedData);

                if (licenseChoice == null)
                {
                    return;
                }

                component.Licenses = [licenseChoice];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing component {PackageUrl}: {Message}", packageUrl, ex.Message);
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