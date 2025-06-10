namespace CdxEnrich.ClearlyDefined
{
    /// <summary>
    ///     Smart Enum for supported package types in ClearlyDefined
    /// </summary>
    public sealed record PackageType(string Name, string Value, Provider DefaultProvider)
    {
        // Static instances of all supported package types with direct assignment of DefaultProvider
        public static readonly PackageType Npm = new(nameof(Npm), "npm", Provider.Npmjs);
        public static readonly PackageType Nuget = new(nameof(Nuget), "nuget", Provider.Nuget);
        public static readonly PackageType Maven = new(nameof(Maven), "maven", Provider.MavenCentral);
        public static readonly PackageType Pypi = new(nameof(Pypi), "pypi", Provider.Pypi);
        public static readonly PackageType Gem = new(nameof(Gem), "gem", Provider.RubyGems);
        public static readonly PackageType Golang = new(nameof(Golang), "golang", Provider.GitHub);
        public static readonly PackageType Debian = new(nameof(Debian), "debian", Provider.Debian);
        public static readonly PackageType CocoaPods = new(nameof(CocoaPods), "cocoapods", Provider.Cocoapods);
        public static readonly PackageType Composer = new(nameof(Composer), "composer", Provider.Packagist);
        public static readonly PackageType Cargo = new(nameof(Cargo), "cargo", Provider.Cratesio);
        public static readonly PackageType GitHubActions = new(nameof(GitHubActions), "githubactions", Provider.GitHub);
        public static readonly PackageType Pod = new(nameof(Pod), "pod", Provider.Cocoapods);
        public static readonly PackageType Crate = new(nameof(Crate), "crate", Provider.Cratesio);

        // Dictionary for fast access by Value
        private static readonly Dictionary<string, PackageType> _lookupByValue =
            new List<PackageType>
            {
                Npm, Nuget, Maven, Pypi, Gem, Golang, Debian,
                CocoaPods, Composer, Cargo, GitHubActions, Pod, Crate
            }.ToDictionary(p => p.Value, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Tries to find a PackageType based on a PURL type
        /// </summary>
        private static bool TryFromPurlType(string purlType, out PackageType? packageType)
        {
            if (string.IsNullOrEmpty(purlType))
            {
                packageType = null;
                return false;
            }

            return _lookupByValue.TryGetValue(purlType, out packageType);
        }

        /// <summary>
        ///     Finds a PackageType based on a PURL type
        /// </summary>
        public static PackageType FromPurlType(string purlType)
        {
            if (TryFromPurlType(purlType, out var packageType))
            {
                return packageType!;
            }

            throw new ArgumentException($"No matching ClearlyDefined package type for PURL type: {purlType}");
        }

        // For records, ToString() is automatically overridden and returns a formatted string with all properties
        // We override it here to return only the name
        public override string ToString()
        {
            return this.Name;
        }
    }
}