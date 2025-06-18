using CycloneDX.Models;
using Microsoft.Extensions.Logging;
using PackageUrl;

namespace CdxEnrich.ClearlyDefined.Rules
{
    internal sealed class OtherLicenseResolveRule(ILogger logger) : ResolveLicenseRuleBase(logger)
    {
        public override bool CanResolve(ClearlyDefinedResponse.LicensedData dataLicensed)
        {
            // This rule applies to any license declaration that contains "OTHER"
            return ContainsOther(dataLicensed.Declared);
        }

        public override LicenseChoice? Resolve(PackageURL packageUrl, ClearlyDefinedResponse.LicensedData dataLicensed)
        {
            var licenseExpressions = dataLicensed.Facets.Core.Discovered.Expressions;

            // Case 1: No expressions, empty expressions, or expressions with unknown license references
            if (licenseExpressions == null ||
                !licenseExpressions.Any() ||
                ContainsUnknownScancodeLicenseReference(licenseExpressions))
            {
                Logger.LogInformation(
                    "Resolved no licenses for package: {PackageUrl} due to 'OTHER' license with missing or invalid expressions",
                    packageUrl);
                return null;
            }

            // Case 2: Valid expressions available
            var joinedLicenseExpression = string.Join(" OR ", licenseExpressions);

            Logger.LogInformation(
                "Resolved license expressions ({LicenseExpressions}) for package: {PackageUrl}",
                joinedLicenseExpression, packageUrl);

            return new LicenseChoice
                {
                    Expression = joinedLicenseExpression
                };
        }
    }
}