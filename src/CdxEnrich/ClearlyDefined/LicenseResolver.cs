using CdxEnrich.ClearlyDefined.Rules;
using CycloneDX.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PackageUrl;

namespace CdxEnrich.ClearlyDefined
{
    public class LicenseResolver
    {
        private readonly ILogger<LicenseResolver> _logger;
        private readonly List<IResolveLicenseRule> _rules = new();

        public LicenseResolver(ILogger<LicenseResolver>? logger = null)
        {
            _logger = logger ?? NullLogger<LicenseResolver>.Instance;

            _rules.Add(new OtherLicenseResolveRule(_logger));
            _rules.Add(new SpdxExpressionResolveRule(_logger));
            _rules.Add(new LicenseIdResolveRule(_logger));
        }

        public LicenseChoice? Resolve(PackageURL packageUrl, ClearlyDefinedResponse.LicensedData dataLicensed)
        {
            var selectedRules =_rules.Where(x => x.CanResolve(dataLicensed)).ToList();
            
            if(selectedRules.Count == 0)
            {
                _logger.LogInformation("No applicable license rules found for package: {PackageUrl}", packageUrl);
                return null;
            }
            if (selectedRules.Count > 1)
            {
                _logger.LogError(
                    "Multiple license rules found ({RuleNames}) for package: {PackageUrl}. Applying no rule to prevent unexpected or incorrect results.",
                    string.Join(", ", selectedRules.Select(r => r.GetType().Name)),
                    packageUrl);
                return null;
            }

            var selectedRule = selectedRules.Single();
            return selectedRule.Resolve(packageUrl, dataLicensed);
        }
    }
}