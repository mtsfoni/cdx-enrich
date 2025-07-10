using Microsoft.Extensions.Logging;

namespace CdxEnrich.ClearlyDefined.Rules
{
    public class ResolveLicenseRuleFactory(ILoggerFactory loggerFactory)
    {
        public IEnumerable<IResolveLicenseRule> Create()
        {
            var rules = new List<IResolveLicenseRule>
            {
                new LicenseIdResolveRule(loggerFactory.CreateLogger<LicenseIdResolveRule>()),
                new SpdxExpressionResolveRule(loggerFactory.CreateLogger<SpdxExpressionResolveRule>()),
            };
            rules.AddRange(LicensePlaceholder.All.Select(placeholder => new PlaceholderLicenseResolveRule(loggerFactory.CreateLogger<PlaceholderLicenseResolveRule>(), placeholder)));
            return rules;
        }
    }
}