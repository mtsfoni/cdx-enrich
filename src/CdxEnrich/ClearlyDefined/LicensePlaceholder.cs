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
        /// Tries to get a LicensePlaceholder by its license identifier.
        /// </summary>
        /// <param name="licenseIdentifier"></param>
        /// <param name="licensePlaceholder"></param>
        /// <returns></returns>
        public static bool TryGetByLicenseIdentifier(string licenseIdentifier, out LicensePlaceholder? licensePlaceholder)
        {
            licensePlaceholder = All.FirstOrDefault(x =>
                x.LicenseIdentifier.Equals(licenseIdentifier, StringComparison.OrdinalIgnoreCase));
            return licensePlaceholder != null;
        }

        // For records, ToString() is automatically overridden and returns a formatted string with all properties
        // We override it here to return only the license identifier
        public override string ToString()
        {
            return this.LicenseIdentifier;
        }
    }
}
