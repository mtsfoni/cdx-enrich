using CycloneDX.Models;
using Microsoft.Extensions.Logging;
using PackageUrl;

namespace CdxEnrich.ClearlyDefined.Rules
{
    /// <summary>
    /// This rule handles license placeholders such as NONE, NOASSERTION and OTHER,
    /// which require an alternative resolution approach.
    /// </summary>
    internal sealed class PlaceholderLicenseResolveRule(ILogger logger, LicensePlaceholder licensePlaceholder)
        : ResolveLicenseRuleBase(logger)
    {
        public override bool CanResolve(ClearlyDefinedResponse.LicensedData dataLicensed)
        {
            return licensePlaceholder.IsInDeclaredLicense(dataLicensed.Declared!);
        }

        public override LicenseChoice? Resolve(PackageURL packageUrl, ClearlyDefinedResponse.LicensedData dataLicensed)
        {
            var licenseExpressions = dataLicensed.Facets!.Core.Discovered.Expressions;

            if (licenseExpressions == null || !this.TryGetJoinedLicenseExpression(licenseExpressions, out var joinedLicenseExpression))
            {
                this.Logger.LogInformation(
                    "Resolved no licenses for package: {PackageUrl} due to placeholder '{LicensePlaceholder}' in declared license with missing or invalid expressions",
                    packageUrl, licensePlaceholder.LicenseIdentifier);
                return null;
            }

            if (joinedLicenseExpression != null &&
                LicensePlaceholder.TryGetByLicenseIdentifier(joinedLicenseExpression,
                    out var licensePlaceholderFromExpression))
            {
                this.Logger.LogInformation(
                    "Resolved no licenses for package: {PackageUrl} due to placeholder '{LicensePlaceholder}' in declared license and expression with license placeholder '{LicensePlaceholderFromExpression}'",
                    packageUrl, licensePlaceholder.LicenseIdentifier, licensePlaceholderFromExpression!.LicenseIdentifier);
                return null;
            }

            this.Logger.LogInformation(
                "Resolved license expressions ({LicenseExpressions}) for package: {PackageUrl}",
                joinedLicenseExpression, packageUrl);

            return new LicenseChoice
            {
                Expression = joinedLicenseExpression
            };
        }
    }
}
