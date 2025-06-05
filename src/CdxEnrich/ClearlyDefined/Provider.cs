using System;
using System.Collections.Generic;
using System.Linq;

namespace CdxEnrich.ClearlyDefined
{
    /// <summary>
    /// Smart Enum für die unterstützten Provider in ClearlyDefined
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

        // Mapping von Pakettypen zu Providern
        private static readonly Dictionary<PackageType, Provider> _typeToProviderMap = new()
        {
            { PackageType.Npm, Npmjs },
            { PackageType.Nuget, Nuget },
            { PackageType.Maven, MavenCentral },
            { PackageType.Pypi, Pypi },
            { PackageType.Gem, RubyGems },
            { PackageType.Golang, GitHub },
            { PackageType.Cargo, Cratesio },
            { PackageType.CocoaPods, Cocoapods },
            { PackageType.Composer, Packagist },
            { PackageType.Debian, Debian }
        };

        // Direktes Mapping von PURL-Typen zu Providern (für Kompatibilität)
        private static readonly Dictionary<string, Provider> _purlTypeToProviderMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "npm", Npmjs },
            { "nuget", Nuget },
            { "maven", MavenCentral },
            { "pypi", Pypi },
            { "gem", RubyGems },
            { "golang", GitHub },
            { "cargo", Cratesio },
            { "cocoapods", Cocoapods },
            { "composer", Packagist },
            { "debian", Debian }
        };

        // Dictionary für schnellen Zugriff nach ApiString
        private static readonly Dictionary<string, Provider> _lookupByApiString = 
            new List<Provider> 
            { 
                AnacondaMain, AnacondaR, Cocoapods, CondaForge, Cratesio,
                Debian, GitHub, GitLab, MavenCentral, MavenGoogle,
                GradlePlugin, Npmjs, Nuget, Packagist, Pypi, RubyGems
            }.ToDictionary(p => p.ApiString);

        /// <summary>
        /// Gibt alle verfügbaren Provider zurück
        /// </summary>
        public static IEnumerable<Provider> All => _lookupByApiString.Values;

        /// <summary>
        /// Versucht, einen Provider anhand seines API-Strings zu finden
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
        /// Findet einen Provider anhand seines API-Strings
        /// </summary>
        public static Provider FromApiString(string apiString)
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
        public static Provider FromPackageType(PackageType packageType)
        {
            if (_typeToProviderMap.TryGetValue(packageType, out var provider))
            {
                return provider;
            }
            
            throw new ArgumentException($"Kein passender Provider für Pakettyp: {packageType.Name}");
        }
        
        /// <summary>
        /// Mappt einen PURL-Typ-String direkt auf den entsprechenden ClearlyDefined-Provider
        /// </summary>
        public static Provider FromPurlType(string purlType)
        {
            if (string.IsNullOrEmpty(purlType))
            {
                throw new ArgumentException("PURL-Typ darf nicht null oder leer sein.");
            }

            // Versuche direktes Mapping
            if (_purlTypeToProviderMap.TryGetValue(purlType, out var provider))
            {
                return provider;
            }
            
            // Wenn nicht gefunden, versuche über PackageType zu gehen
            if (PackageType.TryFromPurlType(purlType, out var packageType) && packageType != null)
            {
                return FromPackageType(packageType);
            }
            
            throw new ArgumentException($"Kein passender Provider für PURL-Typ: {purlType}");
        }

        // Bei Records wird ToString() automatisch überschrieben und gibt einen formatierten String mit allen Properties zurück
        // Wir überschreiben es hier, um nur den Namen zurückzugeben
        public override string ToString() => Name;
    }
}
