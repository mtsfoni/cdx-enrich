using CycloneDX.Models;
using Microsoft.Extensions.Logging;
using PackageUrl;

namespace CdxEnrich.ClearlyDefined
{
    public static class LicenseResolver
    {
        public static LicenseChoice? Resolve(
            ILogger logger,
            PackageURL packageUrl,
            ClearlyDefinedResponse.LicensedData licensedData)
        {
            var declared = licensedData.Declared;
            
            if (string.IsNullOrEmpty(declared))
            {
                logger.LogInformation("No declared license for package: {PackageUrl}", packageUrl);
                return null;
            }

            // Handle placeholders (NONE, NOASSERTION, OTHER) - try discovered expressions
            if (IsLicensePlaceholder(declared))
            {
                return ResolvePlaceholder(logger, packageUrl, licensedData);
            }

            // Handle SPDX expressions (contains operators) or LicenseRef
            if (IsExpression(declared) || IsLicenseRef(declared))
            {
                logger.LogInformation(
                    "Resolved license expression: {DeclaredLicense} for package: {PackageUrl}",
                    declared, packageUrl);
                
                return new LicenseChoice { Expression = declared };
            }

            // Handle simple license ID
            logger.LogInformation(
                "Resolved license ID ({DeclaredLicenseId}) for package: {PackageUrl}",
                declared, packageUrl);
            
            return new LicenseChoice
            {
                License = new License { Id = declared }
            };
        }

        private static LicenseChoice? ResolvePlaceholder(
            ILogger logger,
            PackageURL packageUrl,
            ClearlyDefinedResponse.LicensedData licensedData)
        {
            var licenseExpressions = licensedData.Facets?.Core.Discovered.Expressions;

            if (!TryGetJoinedLicenseExpression(licenseExpressions, out var joinedExpression))
            {
                logger.LogInformation(
                    "Resolved no licenses for package: {PackageUrl} due to placeholder '{Placeholder}' in declared license with missing or invalid expressions",
                    packageUrl, licensedData.Declared);
                return null;
            }

            var containingPlaceholders = LicensePlaceholder.ExtractContaining(joinedExpression);
            if (containingPlaceholders.Any())
            {
                logger.LogInformation(
                    "Resolved no licenses for package: {PackageUrl} due to placeholder '{Placeholder}' in declared license and expression with license placeholders '{ExpressionPlaceholders}'",
                    packageUrl, licensedData.Declared, string.Join(",", containingPlaceholders));
                return null;
            }

            logger.LogInformation(
                "Resolved license expressions ({LicenseExpressions}) for package: {PackageUrl}",
                joinedExpression, packageUrl);

            return new LicenseChoice { Expression = joinedExpression };
        }

        private static bool IsExpression(string declared)
        {
            return declared.Contains(" OR ") ||
                   declared.Contains(" AND ") ||
                   declared.Contains(" WITH ");
        }

        private static bool IsLicenseRef(string declared)
        {
            return declared.StartsWith("LicenseRef-");
        }

        private static bool IsLicensePlaceholder(string declared)
        {
            return LicensePlaceholder.All.Any(x => x.IsInDeclaredLicense(declared));
        }

        private static bool TryGetJoinedLicenseExpression(
            List<string>? licenseExpressions,
            out string? joinedLicenseExpression)
        {
            if (licenseExpressions == null || 
                !licenseExpressions.Any() ||
                ContainsUnknownScancodeLicenseReference(licenseExpressions))
            {
                joinedLicenseExpression = null;
                return false;
            }

            joinedLicenseExpression = string.Join(" OR ", licenseExpressions);
            return true;
        }

        private static bool ContainsUnknownScancodeLicenseReference(List<string> licenseExpressions)
        {
            return licenseExpressions.Exists(expression =>
                expression.Contains("LicenseRef-scancode-unknown-license-reference",
                    StringComparison.OrdinalIgnoreCase));
        }
    }
}
