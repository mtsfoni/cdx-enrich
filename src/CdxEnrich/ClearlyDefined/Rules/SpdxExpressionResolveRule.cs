using CycloneDX.Models;
using Microsoft.Extensions.Logging;
using PackageUrl;

namespace CdxEnrich.ClearlyDefined.Rules
{
    internal sealed class SpdxExpressionResolveRule(ILogger logger) : ResolveLicenseRuleBase(logger)
    {
        public override bool CanResolve(ClearlyDefinedResponse.LicensedData dataLicensed)
        {
            return !this.IsLicensePlaceholder(dataLicensed.Declared!) &&
                   (IsExpression(dataLicensed.Declared!) || IsLicenseRef(dataLicensed.Declared!));
        }

        public override LicenseChoice Resolve(PackageURL packageUrl, ClearlyDefinedResponse.LicensedData dataLicensed)
        {
            Logger.LogInformation(
                "Resolved license expression: {DeclaredLicense} for package: {PackageUrl}",
                dataLicensed.Declared, packageUrl);

            return new LicenseChoice
            {
                Expression = dataLicensed.Declared
            };
        }
    }
}
