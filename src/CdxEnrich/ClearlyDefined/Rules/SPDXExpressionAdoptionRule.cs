using CycloneDX.Models;
using Microsoft.Extensions.Logging;
using PackageUrl;

namespace CdxEnrich.ClearlyDefined.Rules
{
    internal sealed class SPDXExpressionAdoptionRule(ILogger logger) : AdoptionLicenseRuleBase(logger)
    {
        public override bool CanApply(ClearlyDefinedResponse.LicensedData dataLicensed)
        {
            return !ContainsOther(dataLicensed.Declared) && 
                   IsExpression(dataLicensed.Declared);
        }

        public override LicenseChoice? Apply(PackageURL packageUrl, ClearlyDefinedResponse.LicensedData dataLicensed)
        {
            Logger.LogInformation(
                "Using declared license expression: {DeclaredLicense} for package: {PackageUrl}",
                dataLicensed.Declared, packageUrl);

            return new LicenseChoice
            {
                Expression = dataLicensed.Declared
            };
        }
    }
}
