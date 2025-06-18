using CycloneDX.Models;
using PackageUrl;

namespace CdxEnrich.ClearlyDefined.Rules
{
    public interface IAdoptionLicenseRule
    {
        bool CanApply(ClearlyDefinedResponse.LicensedData dataLicensed);
        LicenseChoice? Apply(PackageURL packageUrl, ClearlyDefinedResponse.LicensedData dataLicensed);
    }
}
