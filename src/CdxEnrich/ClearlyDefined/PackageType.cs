namespace CdxEnrich.ClearlyDefined
{
    /// <summary>
    ///     Smart Enum für die unterstützten Pakettypen in ClearlyDefined
    /// </summary>
    public sealed record PackageType(string Name, string Value)
    {
        // Statische Instanzen aller unterstützten Pakettypen
        public static readonly PackageType Npm = new(nameof(Npm), "npm");
        public static readonly PackageType Nuget = new(nameof(Nuget), "nuget");
        public static readonly PackageType Maven = new(nameof(Maven), "maven");
        public static readonly PackageType Pypi = new(nameof(Pypi), "pypi");
        public static readonly PackageType Gem = new(nameof(Gem), "gem");
        public static readonly PackageType Golang = new(nameof(Golang), "golang");
        public static readonly PackageType Debian = new(nameof(Debian), "debian");
        public static readonly PackageType CocoaPods = new(nameof(CocoaPods), "cocoapods");
        public static readonly PackageType Composer = new(nameof(Composer), "composer");
        public static readonly PackageType Cargo = new(nameof(Cargo), "cargo");
        public static readonly PackageType GitHubActions = new(nameof(GitHubActions), "githubactions");
        public static readonly PackageType Pod = new(nameof(Pod), "pod");
        public static readonly PackageType Crate = new(nameof(Crate), "crate");

        // Dictionary für schnellen Zugriff nach Value
        private static readonly Dictionary<string, PackageType> _lookupByValue =
            new List<PackageType>
            {
                Npm, Nuget, Maven, Pypi, Gem, Golang, Debian,
                CocoaPods, Composer, Cargo, GitHubActions, Pod, Crate
            }.ToDictionary(p => p.Value, StringComparer.OrdinalIgnoreCase);

        // Mapping von PURL-Typen zu ClearlyDefined PackageTypes
        private static readonly Dictionary<string, PackageType> _purlTypeToPackageType =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "npm", Npm },
                { "nuget", Nuget },
                { "maven", Maven },
                { "pypi", Pypi },
                { "gem", Gem },
                { "golang", Golang },
                { "debian", Debian },
                { "cocoapods", CocoaPods },
                { "composer", Composer },
                { "cargo", Cargo },
                { "githubactions", GitHubActions },
                { "pod", Pod },
                { "crate", Crate }
            };

        /// <summary>
        ///     Gibt alle verfügbaren Pakettypen zurück
        /// </summary>
        public static IEnumerable<PackageType> All => _lookupByValue.Values;

        /// <summary>
        ///     Versucht, einen Pakettyp anhand seines Value-Strings zu finden
        /// </summary>
        public static bool TryFromValue(string value, out PackageType? packageType)
        {
            if (string.IsNullOrEmpty(value))
            {
                packageType = null;
                return false;
            }

            return _lookupByValue.TryGetValue(value, out packageType);
        }

        /// <summary>
        ///     Findet einen Pakettyp anhand seines Value-Strings
        /// </summary>
        public static PackageType FromValue(string value)
        {
            if (TryFromValue(value, out var packageType))
            {
                return packageType!;
            }

            throw new ArgumentException($"Unbekannter Pakettyp: {value}");
        }

        /// <summary>
        ///     Versucht, einen PackageType anhand eines PURL-Typs zu finden
        /// </summary>
        public static bool TryFromPurlType(string purlType, out PackageType? packageType)
        {
            if (string.IsNullOrEmpty(purlType))
            {
                packageType = null;
                return false;
            }

            return _purlTypeToPackageType.TryGetValue(purlType, out packageType);
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