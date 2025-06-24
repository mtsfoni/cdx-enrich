using CycloneDX.Models;
using Microsoft.Extensions.Logging;
using PackageUrl;

namespace CdxEnrich.ClearlyDefined.Rules
{
    /// <summary>
    /// Abstract base class for license rules that provides common functionality
    /// </summary>
    internal abstract class ResolveLicenseRuleBase(ILogger logger) : IResolveLicenseRule
    {
        protected readonly ILogger Logger = logger;

        public abstract bool CanResolve(ClearlyDefinedResponse.LicensedData dataLicensed);
        
        public abstract LicenseChoice? Resolve(PackageURL packageUrl, ClearlyDefinedResponse.LicensedData dataLicensed);

        /// <summary>
        /// Checks if a license string is an SPDX expression (contains operators)
        /// </summary>
        protected bool IsExpression(string declared)
        {
            return declared.Contains(" OR ") ||
                   declared.Contains(" AND ") ||
                   declared.Contains(" WITH ");
        }
        
        protected bool IsLicenseRef(string declared)
        {
            // Check if the declared license is a valid license reference
            return declared.StartsWith("LicenseRef-");
        }

        /// <summary>
        /// Checks if a license string contains "OTHER"
        /// </summary>
        protected bool ContainsOther(string declared)
        {
            return declared.Contains("OTHER");
        }
        
        /// <summary>
        /// Checks if a license string contains "NONE"
        /// </summary>
        protected bool ContainsNone(string declared)
        {
            return declared.Contains("NONE");
        }
        
        /// <summary>
        /// Checks if a license string contains "NOASSERTION"
        /// </summary>
        protected bool ContainsNoAssertion(string declared)
        {
            return declared.Contains("NOASSERTION");
        }
        

        /// <summary>
        /// Checks if a list of license expressions contains unknown scancode license references
        /// </summary>
        protected bool ContainsUnknownScancodeLicenseReference(List<string> licenseExpressions)
        {
            return licenseExpressions.Exists(expression =>
                expression.Contains("LicenseRef-scancode-unknown-license-reference",
                    StringComparison.OrdinalIgnoreCase));
        }
        
        /// <summary>
        /// Tries to join a list of license expressions into a single expression
        /// </summary>
        /// <param name="licenseExpressions"></param>
        /// <param name="joinedLicenseExpression"></param>
        /// <returns></returns>
        protected bool TryGetJoinedLicenseExpression(List<string> licenseExpressions,
            out string? joinedLicenseExpression)
        {
            if (!licenseExpressions.Any() ||
                this.ContainsUnknownScancodeLicenseReference(licenseExpressions))
            {
                
                joinedLicenseExpression = null;
                return false;
            }
            
            joinedLicenseExpression = string.Join(" OR ", licenseExpressions);
            return true;
        }
    }
}
