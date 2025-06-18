using CycloneDX.Models;
using Microsoft.Extensions.Logging;
using PackageUrl;

namespace CdxEnrich.ClearlyDefined.Rules
{
    /// <summary>
    /// Abstrakte Basisklasse für Lizenzregeln, die gemeinsame Funktionalität bereitstellt
    /// </summary>
    internal abstract class AdoptionLicenseRuleBase(ILogger logger) : IAdoptionLicenseRule
    {
        protected readonly ILogger Logger = logger;

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
}
