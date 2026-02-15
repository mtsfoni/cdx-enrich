namespace CdxEnrich.ClearlyDefined
{
    /// <summary>
    /// Represents license placeholders that require special handling.
    /// </summary>
    public sealed record LicensePlaceholder(string Name, string LicenseIdentifier)
    {
        /// <summary>
        /// This indicates that a human confirmed that there is no license information in the file.
        /// </summary>
        public static readonly LicensePlaceholder None = new(nameof(None), "NONE");
        
        /// <summary>
        /// This indicates that license-like data is found, but that ClearlyDefined cannot identify an SPDX-identified license.
        /// </summary>
        public static readonly LicensePlaceholder NoAssertion = new(nameof(NoAssertion), "NOASSERTION");
        
        /// <summary>
        /// This indicates that a human confirmed that there is license information in the file but that the license is not an SPDX-identified license.
        /// </summary>
        public static readonly LicensePlaceholder Other = new(nameof(Other), "OTHER");

        // Dictionary for fast access by LicenseIdentifier
        private static readonly Dictionary<string, LicensePlaceholder> _lookupByIdentifier =
            new List<LicensePlaceholder>
            {
                None, NoAssertion, Other
            }.ToDictionary(l => l.LicenseIdentifier);

        /// <summary>
        /// Returns all available license placeholders
        /// </summary>
        public static IEnumerable<LicensePlaceholder> All => _lookupByIdentifier.Values;

        /// <summary>
        /// Checks if the specified license text contains the license placeholder
        /// </summary>
        public bool IsInDeclaredLicense(string declared)
        {
            return declared.Contains(LicenseIdentifier);
        }

        /// <summary>
        /// Extracts a list of license placeholders that are contained within the specified license expression.
        /// </summary>
        /// <param name="licenseExpression">The license expression to evaluate.</param>
        /// <returns>A list of license placeholders found within the provided license expression.</returns>
        public static IList<LicensePlaceholder> ExtractContaining(string? licenseExpression)
        {
            if (licenseExpression == null)
            {
                return [];
            }
            
            return All.Where(x => licenseExpression.Contains(x.LicenseIdentifier, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // For records, ToString() is automatically overridden and returns a formatted string with all properties
        // We override it here to return only the license identifier
        public override string ToString()
        {
            return this.LicenseIdentifier;
        }
    }
}
