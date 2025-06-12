namespace CdxEnrich.ClearlyDefined
{
    /// <summary>
    ///     Smart Enum for supported package types in ClearlyDefined
    /// </summary>
    public sealed class PackageType : IEquatable<PackageType>
    {
        public static IList<PackageType> All { get; } = new List<PackageType>();
        public string Name { get; }
        public string Value { get; }
        public Provider DefaultProvider { get; }

        private PackageType(string name, string value, Provider defaultProvider)
        {
            this.Name = name;
            this.Value = value;
            this.DefaultProvider = defaultProvider;
            
            All.Add(this);
        }
        
        // Static instances of all supported package types with direct assignment of DefaultProvider
        public static readonly PackageType Composer = new(nameof(Composer), "composer", Provider.Packagist);
        public static readonly PackageType Conda = new(nameof(Conda), "conda", Provider.CondaForge);
        public static readonly PackageType Condasrc = new(nameof(Condasrc), "condasrc", Provider.CondaForge);
        public static readonly PackageType Crate = new(nameof(Crate), "crate", Provider.Cratesio);
        public static readonly PackageType Deb = new(nameof(Deb), "deb", Provider.Debian);
        public static readonly PackageType Debsrc = new(nameof(Debsrc), "debsrc", Provider.Debian);
        public static readonly PackageType Gem = new(nameof(Gem), "gem", Provider.RubyGems);
        public static readonly PackageType Git = new(nameof(Git), "git", Provider.GitHub);
        public static readonly PackageType Go = new(nameof(Go), "go", Provider.GitHub);
        public static readonly PackageType Maven = new(nameof(Maven), "maven", Provider.MavenCentral);
        public static readonly PackageType Npm = new(nameof(Npm), "npm", Provider.Npmjs);
        public static readonly PackageType Nuget = new(nameof(Nuget), "nuget", Provider.Nuget);
        public static readonly PackageType Pod = new(nameof(Pod), "pod", Provider.Cocoapods);
        public static readonly PackageType Pypi = new(nameof(Pypi), "pypi", Provider.Pypi);
        public static readonly PackageType SourceArchive = new(nameof(SourceArchive), "sourcearchive", Provider.GitHub);

        // Dictionary for fast access by Value
        private static readonly Dictionary<string, PackageType> LookupByValue =
            All.ToDictionary(p => p.Value, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Tries to find a PackageType based on a PURL type
        /// </summary>
        public static bool TryFromPurlType(string purlType, out PackageType? packageType)
        {
            if (string.IsNullOrEmpty(purlType))
            {
                packageType = null;
                return false;
            }

            return LookupByValue.TryGetValue(purlType, out packageType);
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

        public bool Equals(PackageType? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return this.Name == other.Name && this.Value == other.Value && this.DefaultProvider.Equals(other.DefaultProvider);
        }

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj) || obj is PackageType other && this.Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Name, this.Value, this.DefaultProvider);
        }
    }
}