namespace CdxEnrich.ClearlyDefined.Rules
{
    /// <summary>
    /// SmartEnum for special license types that require special handling.
    /// </summary>
    public sealed record SpecialLicense(string Name, string LicenseIdentifier)
    {
        // Static instances of all special license types
        public static readonly SpecialLicense None = new(nameof(None), "NONE");
        public static readonly SpecialLicense NoAssertion = new(nameof(NoAssertion), "NOASSERTION");
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

        // For records, ToString() is automatically overridden and returns a formatted string with all properties
        // We override it here to return only the name
        public override string ToString()
        {
            return this.Name;
        }
    }
}
