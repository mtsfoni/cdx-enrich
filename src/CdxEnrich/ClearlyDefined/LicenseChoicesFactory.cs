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

            _rules.Add(new OtherLicenseAdoptionRule(_logger));
            _rules.Add(new SPDXExpressionAdoptionRule(_logger));
            _rules.Add(new LicenseIdAdoptionRule(_logger));
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
                    "Multiple license rules found ({RuleNames}) for package: {PackageUrl}. Applying no rule to prevent unexpected or incorrect results.",
                    string.Join(", ", selectedRules.Select(r => r.GetType().Name)),
                    packageUrl);
                return null;
            }

            var selectedRule = selectedRules.Single();
            return selectedRule.Apply(packageUrl, dataLicensed);
        }

        #region License Rules

        /// <summary>
        /// Abstrakte Basisklasse für Lizenzregeln, die gemeinsame Funktionalität bereitstellt
        /// </summary>
        internal abstract class AdoptionLicenseRuleBase : IAdoptionLicenseRule
        {
            protected readonly ILogger Logger;

            protected AdoptionLicenseRuleBase(ILogger logger)
            {
                Logger = logger;
            }

            public abstract bool CanApply(ClearlyDefinedResponse.LicensedData dataLicensed);
            
            public abstract List<LicenseChoice>? Apply(PackageURL packageUrl, ClearlyDefinedResponse.LicensedData dataLicensed);

            /// <summary>
            /// Prüft, ob ein Lizenzstring ein SPDX-Ausdruck ist (enthält Operatoren)
            /// </summary>
            protected bool IsExpression(string declared)
            {
                return declared.Contains(" OR ") ||
                       declared.Contains(" AND ") ||
                       declared.Contains(" WITH ");
            }

            /// <summary>
            /// Prüft, ob ein Lizenzstring "OTHER" enthält
            /// </summary>
            protected bool ContainsOther(string declared)
            {
                return declared.Contains("OTHER");
            }

            /// <summary>
            /// Prüft, ob eine Liste von Lizenzausdrücken unbekannte Scancode-Lizenzreferenzen enthält
            /// </summary>
            protected bool ContainsUnknownScancodeLicenseReference(List<string> licenseExpressions)
            {
                return licenseExpressions.Exists(expression =>
                    expression.Contains("LicenseRef-scancode-unknown-license-reference",
                        StringComparison.OrdinalIgnoreCase));
            }
        }

        internal sealed class OtherLicenseAdoptionRule : AdoptionLicenseRuleBase
        {
            public OtherLicenseAdoptionRule(ILogger logger) : base(logger) { }

            public override bool CanApply(ClearlyDefinedResponse.LicensedData dataLicensed)
            {
                // Diese Regel gilt für jede Lizenzdeklaration, die "OTHER" enthält
                return ContainsOther(dataLicensed.Declared);
            }

            public override List<LicenseChoice>? Apply(PackageURL packageUrl, ClearlyDefinedResponse.LicensedData dataLicensed)
            {
                var licenseExpressions = dataLicensed.Facets.Core.Discovered.Expressions;

                // Fall 1: Keine Ausdrücke, leere Ausdrücke oder Ausdrücke mit unbekannten Lizenzreferenzen
                if (licenseExpressions == null || 
                    !licenseExpressions.Any() || 
                    ContainsUnknownScancodeLicenseReference(licenseExpressions))
                {
                    Logger.LogWarning(
                        "No license adopted for package: {PackageUrl} due to 'OTHER' license with missing or invalid expressions",
                        packageUrl);
                    return null;
                }

                // Fall 2: Gültige Ausdrücke vorhanden
                var joinedLicenseExpression = string.Join(" OR ", licenseExpressions);

                Logger.LogInformation(
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
        }

        internal sealed class SPDXExpressionAdoptionRule : AdoptionLicenseRuleBase
        {
            public SPDXExpressionAdoptionRule(ILogger logger) : base(logger) { }

            public override bool CanApply(ClearlyDefinedResponse.LicensedData dataLicensed)
            {
                return !ContainsOther(dataLicensed.Declared) && 
                       IsExpression(dataLicensed.Declared);
            }

            public override List<LicenseChoice> Apply(PackageURL packageUrl, ClearlyDefinedResponse.LicensedData dataLicensed)
            {
                Logger.LogInformation(
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
        }

        internal sealed class LicenseIdAdoptionRule : AdoptionLicenseRuleBase
        {
            public LicenseIdAdoptionRule(ILogger logger) : base(logger) { }

            public override bool CanApply(ClearlyDefinedResponse.LicensedData dataLicensed)
            {
                // Diese Regel gilt für einfache Lizenz-IDs (keine Ausdrücke und nicht "OTHER")
                return !ContainsOther(dataLicensed.Declared) &&
                       !IsExpression(dataLicensed.Declared);
            }

            public override List<LicenseChoice> Apply(PackageURL packageUrl, ClearlyDefinedResponse.LicensedData dataLicensed)
            {
                Logger.LogInformation(
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
        }

        #endregion
    }
}