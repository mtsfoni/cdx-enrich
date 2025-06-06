namespace CdxEnrich.ClearlyDefined
{
    /// <summary>
    ///     Smart Enum für die unterstützten Provider in ClearlyDefined
    /// </summary>
    public sealed record Provider(string Name, string Value)
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

        // Dictionary für schnellen Zugriff nach ApiString
        private static readonly Dictionary<string, Provider> _lookupByApiString =
            new List<Provider>
            {
                AnacondaMain, AnacondaR, Cocoapods, CondaForge, Cratesio,
                Debian, GitHub, GitLab, MavenCentral, MavenGoogle,
                GradlePlugin, Npmjs, Nuget, Packagist, Pypi, RubyGems
            }.ToDictionary(p => p.Value);

        /// <summary>
        ///     Gibt alle verfügbaren Provider zurück
        /// </summary>
        public static IEnumerable<Provider> All => _lookupByApiString.Values;
        

        // Bei Records wird ToString() automatisch überschrieben und gibt einen formatierten String mit allen Properties zurück
        // Wir überschreiben es hier, um nur den Namen zurückzugeben
        public override string ToString()
        {
            return this.Name;
        }
    }
}