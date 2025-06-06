namespace CdxEnrich.ClearlyDefined
{
    /// <summary>
    ///     Smart Enum für die unterstützten Pakettypen in ClearlyDefined
    /// </summary>
    public sealed record PackageType(string Name, string Value, Provider DefaultProvider)
    {
        // Statische Instanzen aller unterstützten Pakettypen mit direkter Zuweisung der DefaultProvider
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

        // Dictionary für schnellen Zugriff nach Value
        private static readonly Dictionary<string, PackageType> _lookupByValue =
            new List<PackageType>
            {
                Npm, Nuget, Maven, Pypi, Gem, Golang, Debian,
                CocoaPods, Composer, Cargo, GitHubActions, Pod, Crate
            }.ToDictionary(p => p.Value, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Versucht, einen PackageType anhand eines PURL-Typs zu finden
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
        ///     Findet einen PackageType anhand eines PURL-Typs
        /// </summary>
        public static PackageType FromPurlType(string purlType)
        {
            if (TryFromPurlType(purlType, out var packageType))
            {
                return packageType!;
            }

            throw new ArgumentException($"Kein passender ClearlyDefined-Pakettyp für PURL-Typ: {purlType}");
        }

        // Bei Records wird ToString() automatisch überschrieben und gibt einen formatierten String mit allen Properties zurück
        // Wir überschreiben es hier, um nur den Namen zurückzugeben
        public override string ToString()
        {
            return this.Name;
        }
    }
}