using CycloneDX.Models;
using PackageUrl;

namespace CdxEnrich.ClearlyDefined
{
    public static class LicenseResolver
    {
        public static LicenseChoice? Resolve(
            PackageURL packageUrl,
            ClearlyDefinedResponse.LicensedData licensedData)
        {
            var declared = licensedData.Declared;
            
            if (string.IsNullOrEmpty(declared))
            {
                Log.Info($"No declared license for package: {packageUrl}");
                return null;
            }

            // Handle placeholders (NONE, NOASSERTION, OTHER) - try discovered expressions
            if (IsLicensePlaceholder(declared))
            {
                return ResolvePlaceholder(packageUrl, licensedData);
            }

            // Handle SPDX expressions (contains operators) or LicenseRef
            if (IsExpression(declared) || IsLicenseRef(declared))
            {
                Log.Info($"Resolved license expression: {declared} for package: {packageUrl}");
                
                return new LicenseChoice { Expression = declared };
            }

            // Handle simple license ID
            Log.Info($"Resolved license ID ({declared}) for package: {packageUrl}");
            
            return new LicenseChoice
            {
                License = new License { Id = declared }
            };
        }

        private static LicenseChoice? ResolvePlaceholder(
            PackageURL packageUrl,
            ClearlyDefinedResponse.LicensedData licensedData)
        {
            var licenseExpressions = licensedData.Facets?.Core.Discovered.Expressions;

            if (!TryGetJoinedLicenseExpression(licenseExpressions, out var joinedExpression))
            {
                Log.Info($"Resolved no licenses for package: {packageUrl} due to placeholder '{licensedData.Declared}' in declared license with missing or invalid expressions");
                return null;
            }

            var containingPlaceholders = LicensePlaceholder.ExtractContaining(joinedExpression);
            if (containingPlaceholders.Any())
            {
                Log.Info($"Resolved no licenses for package: {packageUrl} due to placeholder '{licensedData.Declared}' in declared license and expression with license placeholders '{string.Join(",", containingPlaceholders)}'");
                return null;
            }

            Log.Info($"Resolved license expressions ({joinedExpression}) for package: {packageUrl}");

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
