using CycloneDX.Models;
using Microsoft.Extensions.Logging;
using PackageUrl;

namespace CdxEnrich.ClearlyDefined.Rules
{
    /// <summary>
    /// This rule handles special license types such as NONE, NOASSERTION and OTHER,
    /// which require an alternative resolution approach.
    /// </summary>
    internal sealed class FallbackLicenseResolveRule(ILogger logger, SpecialLicense specialLicense)
        : ResolveLicenseRuleBase(logger)
    {
        public override bool CanResolve(ClearlyDefinedResponse.LicensedData dataLicensed)
        {
            return specialLicense.IsInDeclaredLicense(dataLicensed.Declared!);
        }

        public override LicenseChoice? Resolve(PackageURL packageUrl, ClearlyDefinedResponse.LicensedData dataLicensed)
        {
            var licenseExpressions = dataLicensed.Facets!.Core.Discovered.Expressions;

            if (licenseExpressions == null || !this.TryGetJoinedLicenseExpression(licenseExpressions, out var joinedLicenseExpression))
            {
                this.Logger.LogInformation(
                    "Resolved no licenses for package: {PackageUrl} due to '{SpecialLicense}' license with missing or invalid expressions",
                    packageUrl, specialLicense.LicenseIdentifier);
                return null;
            }

            if (joinedLicenseExpression != null &&
                SpecialLicense.TryGetByLicenseIdentifier(joinedLicenseExpression,
                    out var specialLicenseFromExpression))
            {
                this.Logger.LogInformation(
                    "Resolved no licenses for package: {PackageUrl} due to '{SpecialLicense}' declared license and expression with special license '{SpecialLicenseFromExpression}'",
                    packageUrl, specialLicense.LicenseIdentifier, specialLicenseFromExpression!.LicenseIdentifier);
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
