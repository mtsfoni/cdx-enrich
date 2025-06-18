using CycloneDX.Models;
using Microsoft.Extensions.Logging;
using PackageUrl;

namespace CdxEnrich.ClearlyDefined.Rules
{
    internal sealed class OtherLicenseResolveRule(ILogger logger) : ResolveLicenseRuleBase(logger)
    {
        public override bool CanResolve(ClearlyDefinedResponse.LicensedData dataLicensed)
        {
            // Diese Regel gilt für jede Lizenzdeklaration, die "OTHER" enthält
            return ContainsOther(dataLicensed.Declared);
        }

        public override LicenseChoice? Resolve(PackageURL packageUrl, ClearlyDefinedResponse.LicensedData dataLicensed)
        {
            var licenseExpressions = dataLicensed.Facets.Core.Discovered.Expressions;

            // Fall 1: Keine Ausdrücke, leere Ausdrücke oder Ausdrücke mit unbekannten Lizenzreferenzen
            if (licenseExpressions == null ||
                !licenseExpressions.Any() ||
                ContainsUnknownScancodeLicenseReference(licenseExpressions))
            {
                Logger.LogWarning(
                    "No license adopted for package: {PackageUrl} due to 'OTHER' license with missing or invalid expressions",
                    packageUrl);
                return null;
            }

            // Fall 2: Gültige Ausdrücke vorhanden
            var joinedLicenseExpression = string.Join(" OR ", licenseExpressions);

            Logger.LogInformation(
                "Using license expressions ({LicenseExpressions}) for package: {PackageUrl}",
                joinedLicenseExpression, packageUrl);

            return new LicenseChoice
                {
                    Expression = joinedLicenseExpression
                };
        }
    }
}