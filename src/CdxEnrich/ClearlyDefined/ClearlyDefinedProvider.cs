using System;
using System.Collections.Generic;
using System.Linq;

namespace CdxEnrich.ClearlyDefined
{
    /// <summary>
    /// Smart Enum für die unterstützten Provider in ClearlyDefined
    /// </summary>
    public sealed record ClearlyDefinedProvider(string Name, string ApiString)
    {
        // Statische Instanzen aller Provider
        public static readonly ClearlyDefinedProvider AnacondaMain = new(nameof(AnacondaMain), "anaconda-main");
        public static readonly ClearlyDefinedProvider AnacondaR = new(nameof(AnacondaR), "anaconda-r");
        public static readonly ClearlyDefinedProvider Cocoapods = new(nameof(Cocoapods), "cocoapods");
        public static readonly ClearlyDefinedProvider CondaForge = new(nameof(CondaForge), "conda-forge");
        public static readonly ClearlyDefinedProvider Cratesio = new(nameof(Cratesio), "cratesio");
        public static readonly ClearlyDefinedProvider Debian = new(nameof(Debian), "debian");
        public static readonly ClearlyDefinedProvider GitHub = new(nameof(GitHub), "github");
        public static readonly ClearlyDefinedProvider GitLab = new(nameof(GitLab), "gitlab");
        public static readonly ClearlyDefinedProvider MavenCentral = new(nameof(MavenCentral), "mavencentral");
        public static readonly ClearlyDefinedProvider MavenGoogle = new(nameof(MavenGoogle), "mavengoogle");
        public static readonly ClearlyDefinedProvider GradlePlugin = new(nameof(GradlePlugin), "gradleplugin");
        public static readonly ClearlyDefinedProvider Npmjs = new(nameof(Npmjs), "npmjs");
        public static readonly ClearlyDefinedProvider Nuget = new(nameof(Nuget), "nuget");
        public static readonly ClearlyDefinedProvider Packagist = new(nameof(Packagist), "packagist");
        public static readonly ClearlyDefinedProvider Pypi = new(nameof(Pypi), "pypi");
        public static readonly ClearlyDefinedProvider RubyGems = new(nameof(RubyGems), "rubygems");

        // Mapping von Pakettypen zu Providern
        private static readonly Dictionary<string, ClearlyDefinedProvider> TypeToProviderMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "npm", Npmjs },
            { "nuget", Nuget },
            { "maven", MavenCentral },
            { "pypi", Pypi },
            { "gem", RubyGems },
            { "golang", GitHub }
        };

        // Dictionary für schnellen Zugriff nach ApiString
        private static readonly Dictionary<string, ClearlyDefinedProvider> _lookupByApiString = 
            new List<ClearlyDefinedProvider> 
            { 
                AnacondaMain, AnacondaR, Cocoapods, CondaForge, Cratesio,
                Debian, GitHub, GitLab, MavenCentral, MavenGoogle,
                GradlePlugin, Npmjs, Nuget, Packagist, Pypi, RubyGems
            }.ToDictionary(p => p.ApiString);

        /// <summary>
        /// Gibt alle verfügbaren Provider zurück
        /// </summary>
        public static IEnumerable<ClearlyDefinedProvider> All => _lookupByApiString.Values;

        /// <summary>
        /// Versucht, einen Provider anhand seines API-Strings zu finden
        /// </summary>
        public static bool TryFromApiString(string apiString, out ClearlyDefinedProvider? provider)
        {
            if (string.IsNullOrEmpty(apiString))
            {
                provider = null;
                return false;
            }

            return _lookupByApiString.TryGetValue(apiString, out provider);
        }

        /// <summary>
        /// Findet einen Provider anhand seines API-Strings
        /// </summary>
        public static ClearlyDefinedProvider FromApiString(string apiString)
        {
            if (TryFromApiString(apiString, out var provider))
            {
                return provider!;
            }

            throw new ArgumentException($"Unbekannter Provider-API-String: {apiString}");
        }

        /// <summary>
        /// Mappt einen Pakettyp auf den entsprechenden ClearlyDefined-Provider
        /// </summary>
        public static ClearlyDefinedProvider FromPackageType(string type)
        {
            if (TypeToProviderMap.TryGetValue(type, out var provider))
            {
                return provider;
            }
            
            throw new ArgumentException($"Unbekannter Pakettyp: {type}");
        }

        // Bei Records wird ToString() automatisch überschrieben und gibt einen formatierten String mit allen Properties zurück
        // Wir überschreiben es hier, um nur den Namen zurückzugeben
        public override string ToString() => Name;
    }
}
