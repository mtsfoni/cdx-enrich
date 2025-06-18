using CycloneDX.Models;
using PackageUrl;

namespace CdxEnrich.ClearlyDefined.Rules
{
    public interface IAdoptionLicenseRule
    {
        bool CanApply(ClearlyDefinedResponse.LicensedData dataLicensed);
        List<LicenseChoice>? Apply(PackageURL packageUrl, ClearlyDefinedResponse.LicensedData dataLicensed);
    }
}
