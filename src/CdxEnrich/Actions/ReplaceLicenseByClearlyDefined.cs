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
        private readonly IClearlyDefinedClient _clearlyDefinedClient;
        
        private static readonly string ModuleName = nameof(ReplaceLicenseByClearlyDefined);

        public ReplaceLicenseByClearlyDefined(
            IClearlyDefinedClient clearlyDefinedClient)
        {
            _clearlyDefinedClient = clearlyDefinedClient;
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


        public static Result<ConfigRoot> CheckConfig(ConfigRoot config)
        {
            return RefMustNotBeNullOrEmpty(config)
                .Bind(RefsMustBeUnique);
        }

        public Result<InputTuple> Execute(InputTuple inputs)
        {
            try
            {
                var tasks = new List<Task>();

                var configEntries = inputs.Config.ReplaceLicenseByClearlyDefined?
                    .Where(item => item.Ref != null)
                    .ToList();
                
                if(configEntries == null)
                {
                    return new Ok<InputTuple>(inputs);
                }

                foreach (var configEntry in configEntries)
                {
                    var configEntryRef = configEntry.Ref!;
                    var component = GetComponentByBomRef(inputs.Bom, configEntryRef);
                    
                    // Graceful: Component not found? Skip it
                    if (component == null)
                    {
                        Log.Warn($"Component with BomRef '{configEntryRef}' not found in BOM, skipping");
                        continue;
                    }
                    
                    // Graceful: No PURL? Skip it
                    if (string.IsNullOrEmpty(component.Purl))
                    {
                        Log.Warn($"Component with BomRef '{configEntryRef}' has no PURL set, skipping");
                        continue;
                    }
                    
                    // Graceful: Invalid PURL format? Skip it
                    PackageURL? packageUrl;
                    try
                    {
                        packageUrl = new PackageURL(component.Purl);
                    }
                    catch
                    {
                        Log.Warn($"Invalid PURL format '{component.Purl}' for BomRef '{configEntryRef}', skipping");
                        continue;
                    }
                    
                    // Graceful: Unsupported package type? Skip it
                    if (!PackageType.TryFromPurlType(packageUrl.Type, out var packageType) || packageType == null)
                    {
                        Log.Info($"Package type '{packageUrl.Type}' is not supported by ClearlyDefined for BomRef '{configEntryRef}', skipping");
                        continue;
                    }
                    
                    if (!IsSupported(packageType))
                    {
                        Log.Info($"Package type '{packageType.Name}' is currently not supported by cdx-enrich for BomRef '{configEntryRef}', skipping");
                        continue;
                    }
                    
                    var provider = packageType.DefaultProvider;
                    tasks.Add(ProcessComponentAsync(component, packageUrl, provider));
                }

                // If API is down, network fails, etc. - catch and return Error
                Task.WhenAll(tasks).GetAwaiter().GetResult();

                return new Ok<InputTuple>(inputs);
            }
            catch (Exception ex)
            {
                return ExternalApiError.Create<InputTuple>("ClearlyDefined", ex.Message);
            }
        }

        private async Task ProcessComponentAsync(Component component, PackageURL packageUrl, Provider provider)
        {
            // Fetching license data from ClearlyDefined
            var licensedData = await _clearlyDefinedClient.GetClearlyDefinedLicensedDataAsync(packageUrl, provider);
        
            if (licensedData?.Declared == null || licensedData.Facets == null)
            {
                Log.Info($"No license data found for package: {packageUrl}");
                return;
            }
        
            // Using the static resolver to determine the LicenseChoice
            var licenseChoice = LicenseResolver.Resolve(packageUrl, licensedData);

            if (licenseChoice == null)
            {
                return;
            }

            component.Licenses = [licenseChoice];
        }
    }
}