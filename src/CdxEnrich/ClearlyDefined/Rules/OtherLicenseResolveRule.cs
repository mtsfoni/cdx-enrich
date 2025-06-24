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

            if (licenseExpressions == null || !this.TryGetJoinedLicenseExpression(licenseExpressions, out var joinedLicenseExpression))
            {
                this.Logger.LogInformation(
                    "Resolved no licenses for package: {PackageUrl} due to 'OTHER' license with missing or invalid expressions",
                    packageUrl);
                return null;
            }
            
            if (joinedLicenseExpression == dataLicensed.Declared)
            {
                this.Logger.LogInformation(
                    "Resolved no licenses for package: {PackageUrl} due to 'OTHER' license with same expression as declared",
                    packageUrl);
                return null;
            }

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