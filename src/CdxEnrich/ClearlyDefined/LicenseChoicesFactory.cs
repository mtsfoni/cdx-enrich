using CdxEnrich.ClearlyDefined.Rules;
using CycloneDX.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PackageUrl;

namespace CdxEnrich.ClearlyDefined
{
    public class LicenseChoicesFactory
    {
        private readonly ILogger<LicenseChoicesFactory> _logger;
        private readonly List<IAdoptionLicenseRule> _rules = new();

        public LicenseChoicesFactory(ILogger<LicenseChoicesFactory>? logger = null)
        {
            _logger = logger ?? NullLogger<LicenseChoicesFactory>.Instance;

            _rules.Add(new OtherLicenseAdoptionRule(_logger));
            _rules.Add(new SPDXExpressionAdoptionRule(_logger));
            _rules.Add(new LicenseIdAdoptionRule(_logger));
        }

        public LicenseChoice? Create(PackageURL packageUrl, ClearlyDefinedResponse.LicensedData dataLicensed)
        {
            var selectedRules =_rules.Where(x => x.CanApply(dataLicensed)).ToList();
            
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
            return selectedRule.Apply(packageUrl, dataLicensed);
        }
    }
}