using CycloneDX.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PackageUrl;

namespace CdxEnrich.ClearlyDefined
{
    public class LicenseChoicesFactory(ILogger<LicenseChoicesFactory>? logger = null)
    {
        private readonly ILogger<LicenseChoicesFactory> _logger = logger ?? NullLogger<LicenseChoicesFactory>.Instance;

        public List<LicenseChoice>? Create(PackageURL packageUrl,
            ClearlyDefinedResponse.LicensedData dataLicensed)
        {
            if (dataLicensed.Declared.Contains("OTHER"))
            {
                if (!this.TryGetLicenseFromExpressions(packageUrl, dataLicensed, out var joinedLicenseExpression))
                {
                    return null;
                }

                return
                [
                    new LicenseChoice
                    {
                        Expression = joinedLicenseExpression
                    }
                ];
            }

            if (this.IsExpression(dataLicensed.Declared))
            {
                return
                [
                    new LicenseChoice
                    {
                        Expression = dataLicensed.Declared
                    }
                ];
            }

            this._logger.LogInformation("Using declared license ID: {DeclaredLicenseId} for package: {PackageUrl}",
                dataLicensed.Declared, packageUrl);
            return
            [
                new LicenseChoice
                {
                    License = new License
                    {
                        Id = dataLicensed.Declared,
                    }
                }
            ];

        }

        private bool TryGetLicenseFromExpressions(PackageURL packageUrl,
            ClearlyDefinedResponse.LicensedData dataLicensed,
            out string? joinedLicenseExpression)
        {
            var licenseExpressions = dataLicensed.Facets.Core.Discovered.Expressions;
            if (licenseExpressions == null || !licenseExpressions.Any())
            {
                this._logger.LogWarning("No license expressions found for package: {PackageUrl}", packageUrl);
                joinedLicenseExpression = null;
                return false;
            }

            if (this.ContainsUnknownScancodeLicenseReference(licenseExpressions))
            {
                this._logger.LogWarning(
                    "Ignoring 'LicenseRef-scancode-unknown-license-reference' in license expressions for package: {PackageUrl}",
                    packageUrl);
                joinedLicenseExpression = null;
                return false;
            }

            joinedLicenseExpression = string.Join(" OR ", licenseExpressions);

            this._logger.LogInformation(
                "Using license expressions ({LicenseExpressions}) for package: {PackageUrl}",
                joinedLicenseExpression, packageUrl);
            return true;
        }

        private bool ContainsUnknownScancodeLicenseReference(List<string> licenseExpressions)
        {
            // Check if any of the license expressions contain the 'LicenseRef-scancode-unknown-license-reference'
            return licenseExpressions.Exists(expression =>
                expression.Contains("LicenseRef-scancode-unknown-license-reference",
                    StringComparison.OrdinalIgnoreCase));
        }

        private bool IsExpression(string declared)
        {
            // Check if the declared license is a valid SPDX expression}
            return declared.Contains(" OR ") ||
                   declared.Contains(" AND ") ||
                   declared.Contains(" WITH ");
        }
    }
}