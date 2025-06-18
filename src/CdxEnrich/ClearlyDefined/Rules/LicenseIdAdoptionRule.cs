using CycloneDX.Models;
using Microsoft.Extensions.Logging;
using PackageUrl;

namespace CdxEnrich.ClearlyDefined.Rules
{
    internal sealed class LicenseIdAdoptionRule(ILogger logger) : AdoptionLicenseRuleBase(logger)
    {
        public override bool CanApply(ClearlyDefinedResponse.LicensedData dataLicensed)
        {
            // Diese Regel gilt für einfache Lizenz-IDs (keine Ausdrücke und nicht "OTHER")
            return !ContainsOther(dataLicensed.Declared) &&
                   !IsExpression(dataLicensed.Declared);
        }

        public override LicenseChoice Apply(PackageURL packageUrl, ClearlyDefinedResponse.LicensedData dataLicensed)
        {
            Logger.LogInformation(
                "Using declared license ID: {DeclaredLicenseId} for package: {PackageUrl}",
                dataLicensed.Declared, packageUrl);

            return new LicenseChoice
                {
                    License = new License
                    {
                        Id = dataLicensed.Declared,
                    }
                };
        }
    }
}