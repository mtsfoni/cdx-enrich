using CycloneDX.Models;
using Microsoft.Extensions.Logging;
using PackageUrl;

namespace CdxEnrich.ClearlyDefined.Rules
{
    internal sealed class LicenseIdResolveRule(ILogger logger) : ResolveLicenseRuleBase(logger)
    {
        public override bool CanResolve(ClearlyDefinedResponse.LicensedData dataLicensed)
        {
            // This rule applies to simple license IDs (no expressions and not "OTHER")
            return !ContainsOther(dataLicensed.Declared) &&
                   !ContainsNone(dataLicensed.Declared) &&
                   !ContainsNoAssertion(dataLicensed.Declared) && 
                   !IsExpression(dataLicensed.Declared) &&
                   !IsLicenseRef(dataLicensed.Declared);
        }

        public override LicenseChoice Resolve(PackageURL packageUrl, ClearlyDefinedResponse.LicensedData dataLicensed)
        {
            Logger.LogInformation(
                "Resolved license ID: {DeclaredLicenseId} for package: {PackageUrl}",
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