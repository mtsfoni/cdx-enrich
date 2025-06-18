using CycloneDX.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PackageUrl;

namespace CdxEnrich.ClearlyDefined
{
    public interface IAdoptionLicenseRule
    {
        bool CanApply(ClearlyDefinedResponse.LicensedData dataLicensed);
        List<LicenseChoice>? Apply(PackageURL packageUrl, ClearlyDefinedResponse.LicensedData dataLicensed);
    }

    public class LicenseChoicesFactory
    {
        private readonly ILogger<LicenseChoicesFactory> _logger;
        private readonly List<IAdoptionLicenseRule> _rules = new();

        public LicenseChoicesFactory(ILogger<LicenseChoicesFactory>? logger = null)
        {
            _logger = logger ?? NullLogger<LicenseChoicesFactory>.Instance;

            // Add default rules in priority order
            _rules.Add(new OtherAdoptionLicenseWithUnknownRefRule(_logger));
            _rules.Add(new OtherAdoptionLicenseWithExpressionsRule(_logger));
            _rules.Add(new ExpressionAdoptionLicenseRule(_logger));
            _rules.Add(new SimpleAdoptionLicenseIdRule(_logger));
        }

        public List<LicenseChoice>? Create(PackageURL packageUrl, ClearlyDefinedResponse.LicensedData dataLicensed)
        {
            var selectedRules =_rules.Where(x => x.CanApply(dataLicensed)).ToList();
            
            if(selectedRules.Count == 0)
            {
                _logger.LogInformation("No applicable license rules found for package: {PackageUrl}", packageUrl);
                return null;
            }
            if (selectedRules.Count > 1)
            {
                _logger.LogError(
                    "Multiple license rules found for package: {PackageUrl}. Applying no rule to prevent unexpected or incorrect results.",
                    packageUrl);
                return null;
            }

            var selectedRule = selectedRules.Single();
            return selectedRule.Apply(packageUrl, dataLicensed);
        }

        #region License Rules

        internal sealed class OtherAdoptionLicenseWithUnknownRefRule(ILogger logger) : IAdoptionLicenseRule
        {
            public bool CanApply(ClearlyDefinedResponse.LicensedData dataLicensed)
            {
                var licenseExpressions = dataLicensed.Facets.Core.Discovered.Expressions;

                return dataLicensed.Declared.Contains("OTHER") &&
                       (licenseExpressions == null ||
                        !licenseExpressions.Any() ||
                        ContainsUnknownScancodeLicenseReference(licenseExpressions));
            }

            public List<LicenseChoice>? Apply(PackageURL packageUrl, ClearlyDefinedResponse.LicensedData dataLicensed)
            {
                logger.LogWarning(
                    "No license adopted for package: {PackageUrl} due to 'OTHER' license with missing or invalid expressions",
                    packageUrl);
                return null;
            }

            private bool ContainsUnknownScancodeLicenseReference(List<string> licenseExpressions)
            {
                return licenseExpressions.Exists(expression =>
                    expression.Contains("LicenseRef-scancode-unknown-license-reference",
                        StringComparison.OrdinalIgnoreCase));
            }
        }

        internal sealed  class OtherAdoptionLicenseWithExpressionsRule(ILogger logger) : IAdoptionLicenseRule
        {
            public bool CanApply(ClearlyDefinedResponse.LicensedData dataLicensed)
            {
                var licenseExpressions = dataLicensed.Facets.Core.Discovered.Expressions;

                return dataLicensed.Declared.Contains("OTHER") &&
                       licenseExpressions != null &&
                       licenseExpressions.Any() &&
                       !ContainsUnknownScancodeLicenseReference(licenseExpressions);
            }

            public List<LicenseChoice> Apply(PackageURL packageUrl, ClearlyDefinedResponse.LicensedData dataLicensed)
            {
                var licenseExpressions = dataLicensed.Facets.Core.Discovered.Expressions;
                var joinedLicenseExpression = string.Join(" OR ", licenseExpressions);

                logger.LogInformation(
                    "Using license expressions ({LicenseExpressions}) for package: {PackageUrl}",
                    joinedLicenseExpression, packageUrl);

                return
                [
                    new LicenseChoice
                    {
                        Expression = joinedLicenseExpression
                    }
                ];
            }

            private bool ContainsUnknownScancodeLicenseReference(List<string> licenseExpressions)
            {
                return licenseExpressions.Exists(expression =>
                    expression.Contains("LicenseRef-scancode-unknown-license-reference",
                        StringComparison.OrdinalIgnoreCase));
            }
        }

        internal sealed  class ExpressionAdoptionLicenseRule(ILogger logger) : IAdoptionLicenseRule
        {
            public bool CanApply(ClearlyDefinedResponse.LicensedData dataLicensed)
            {
                return IsExpression(dataLicensed.Declared);
            }

            public List<LicenseChoice> Apply(PackageURL packageUrl, ClearlyDefinedResponse.LicensedData dataLicensed)
            {
                logger.LogInformation(
                    "Using declared license expression: {DeclaredLicense} for package: {PackageUrl}",
                    dataLicensed.Declared, packageUrl);

                return
                [
                    new LicenseChoice
                    {
                        Expression = dataLicensed.Declared
                    }
                ];
            }

            private bool IsExpression(string declared)
            {
                return declared.Contains(" OR ") ||
                       declared.Contains(" AND ") ||
                       declared.Contains(" WITH ");
            }
        }

        internal sealed  class SimpleAdoptionLicenseIdRule(ILogger logger) : IAdoptionLicenseRule
        {
            public bool CanApply(ClearlyDefinedResponse.LicensedData dataLicensed)
            {
                // This rule applies to any simple license ID (not an expression and not "OTHER")
                return !dataLicensed.Declared.Contains("OTHER") &&
                       !IsExpression(dataLicensed.Declared);
            }

            public List<LicenseChoice> Apply(PackageURL packageUrl, ClearlyDefinedResponse.LicensedData dataLicensed)
            {
                logger.LogInformation(
                    "Using declared license ID: {DeclaredLicenseId} for package: {PackageUrl}",
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

            private bool IsExpression(string declared)
            {
                return declared.Contains(" OR ") ||
                       declared.Contains(" AND ") ||
                       declared.Contains(" WITH ");
            }
        }

        #endregion
    }
}