namespace CdxEnrich.PackageUrl
{
    /// <summary>
    ///     Smart Enum für die unterstützten Provider in ClearlyDefined
    /// </summary>
    public sealed record Provider(string Name, string ApiString)
    {
        // Statische Instanzen aller Provider
        public static readonly Provider AnacondaMain = new(nameof(AnacondaMain), "anaconda-main");
        public static readonly Provider AnacondaR = new(nameof(AnacondaR), "anaconda-r");
        public static readonly Provider Cocoapods = new(nameof(Cocoapods), "cocoapods");
        public static readonly Provider CondaForge = new(nameof(CondaForge), "conda-forge");
        public static readonly Provider Cratesio = new(nameof(Cratesio), "cratesio");
        public static readonly Provider Debian = new(nameof(Debian), "debian");
        public static readonly Provider GitHub = new(nameof(GitHub), "github");
        public static readonly Provider GitLab = new(nameof(GitLab), "gitlab");
        public static readonly Provider MavenCentral = new(nameof(MavenCentral), "mavencentral");
        public static readonly Provider MavenGoogle = new(nameof(MavenGoogle), "mavengoogle");
        public static readonly Provider GradlePlugin = new(nameof(GradlePlugin), "gradleplugin");
        public static readonly Provider Npmjs = new(nameof(Npmjs), "npmjs");
        public static readonly Provider Nuget = new(nameof(Nuget), "nuget");
        public static readonly Provider Packagist = new(nameof(Packagist), "packagist");
        public static readonly Provider Pypi = new(nameof(Pypi), "pypi");
        public static readonly Provider RubyGems = new(nameof(RubyGems), "rubygems");

        // Dictionary für schnellen Zugriff nach API-String
        private static readonly Dictionary<string, Provider> _lookupByApiString =
            new List<Provider>
            {
                AnacondaMain, AnacondaR, Cocoapods, CondaForge, Cratesio,
                Debian, GitHub, GitLab, MavenCentral, MavenGoogle,
                GradlePlugin, Npmjs, Nuget, Packagist, Pypi, RubyGems
            }.ToDictionary(p => p.ApiString);

        /// <summary>
        ///     Gibt alle verfügbaren Provider zurück
        /// </summary>
        public static IEnumerable<Provider> All => _lookupByApiString.Values;

        /// <summary>
        ///     Versucht, einen Provider anhand seines API-Strings zu finden
        /// </summary>
        public static bool TryFromApiString(string apiString, out Provider? provider)
        {
            if (string.IsNullOrEmpty(apiString))
            {
                provider = null;
                return false;
            }

            return _lookupByApiString.TryGetValue(apiString, out provider);
        }

        /// <summary>
        ///     Findet einen Provider anhand seines API-Strings
        /// </summary>
        public static Provider FromApiString(string apiString)
        {
            if (TryFromApiString(apiString, out var provider))
            {
                return provider!;
            }

            throw new ArgumentException($"Unbekannter Provider-API-String: {apiString}");
        }
        
        public override string ToString()
        {
            return this.Name;
        }
    }
}