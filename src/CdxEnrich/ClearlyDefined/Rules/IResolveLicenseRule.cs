using CycloneDX.Models;
using PackageUrl;

namespace CdxEnrich.ClearlyDefined.Rules
{
    public interface IResolveLicenseRule
    {
        bool CanResolve(ClearlyDefinedResponse.LicensedData dataLicensed);
        LicenseChoice? Resolve(PackageURL packageUrl, ClearlyDefinedResponse.LicensedData dataLicensed);
    }
}
