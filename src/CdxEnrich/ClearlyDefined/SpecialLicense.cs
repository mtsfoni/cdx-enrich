namespace CdxEnrich.ClearlyDefined
{
    /// <summary>
    /// SmartEnum for special license types that require special handling.
    /// </summary>
    public sealed record SpecialLicense(string Name, string LicenseIdentifier)
    {
        /// <summary>
        /// This indicates that a human confirmed that there is no license information in the file.
        /// </summary>
        public static readonly SpecialLicense None = new(nameof(None), "NONE");
        
        /// <summary>
        /// This indicates that license-like data is found, but that ClearlyDefined cannot identify an SPDX-identified license.
        /// </summary>
        public static readonly SpecialLicense NoAssertion = new(nameof(NoAssertion), "NOASSERTION");
        
        /// <summary>
        /// This indicates that a human confirmed that there is license information in the file but that the license is not an SPDX-identified license.
        /// </summary>
        public static readonly SpecialLicense Other = new(nameof(Other), "OTHER");

        // Dictionary for fast access by LicenseIdentifier
        private static readonly Dictionary<string, SpecialLicense> _lookupByIdentifier =
            new List<SpecialLicense>
            {
                None, NoAssertion, Other
            }.ToDictionary(l => l.LicenseIdentifier);

        /// <summary>
        /// Returns all available special license types
        /// </summary>
        public static IEnumerable<SpecialLicense> All => _lookupByIdentifier.Values;

        /// <summary>
        /// Checks if the specified license text contains the special license type
        /// </summary>
        public bool IsInDeclaredLicense(string declared)
        {
            return declared.Contains(LicenseIdentifier);
        }
        
        /// <summary>
        /// Tries to get a SpecialLicense by its license identifier.
        /// </summary>
        /// <param name="licenseIdentifier"></param>
        /// <param name="specialLicense"></param>
        /// <returns></returns>
        public static bool TryGetByLicenseIdentifier(string licenseIdentifier, out SpecialLicense? specialLicense)
        {
            specialLicense = All.FirstOrDefault(x =>
                x.LicenseIdentifier.Equals(licenseIdentifier, StringComparison.OrdinalIgnoreCase));
            return specialLicense != null;
        }

        // For records, ToString() is automatically overridden and returns a formatted string with all properties
        // We override it here to return only the license identifier
        public override string ToString()
        {
            return this.LicenseIdentifier;
        }
    }
}
