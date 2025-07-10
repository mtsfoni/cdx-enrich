using CdxEnrich.ClearlyDefined.Rules;
using CycloneDX.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PackageUrl;

namespace CdxEnrich.ClearlyDefined
{
    public interface ILicenseResolver
    {
        LicenseChoice? Resolve(PackageURL packageUrl, ClearlyDefinedResponse.LicensedData dataLicensed);
    }
    
    public class LicenseResolver(ILogger<LicenseResolver> logger, ResolveLicenseRuleFactory ruleFactory)
        : ILicenseResolver
    {
        private readonly IEnumerable<IResolveLicenseRule> _rules = ruleFactory.Create();

        public LicenseChoice? Resolve(PackageURL packageUrl, ClearlyDefinedResponse.LicensedData dataLicensed)
        {
            var selectedRules =_rules.Where(x => x.CanResolve(dataLicensed)).ToList();
            
            if(selectedRules.Count == 0)
            {
                logger.LogInformation("No applicable license rules found for package: {PackageUrl}", packageUrl);
                return null;
            }
            if (selectedRules.Count > 1)
            {
                logger.LogError(
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