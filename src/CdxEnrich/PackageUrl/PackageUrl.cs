using System;
using System.Collections.Generic;
using System.Linq;

namespace CdxEnrich.PackageUrl
{
    /// <summary>
    /// Repräsentiert eine Package URL (PURL) nach der Spezifikation
    /// https://github.com/package-url/purl-spec
    /// </summary>
    public class PackageUrl
    {
        // Mapping von Pakettypen zu Providern
        private static readonly Dictionary<string, Provider> TypeToProviderMap = new (StringComparer.OrdinalIgnoreCase)
        {
            { "npm", Provider.Npmjs },
            { "nuget", Provider.Nuget },
            { "maven", Provider.MavenCentral },
            { "pypi", Provider.Pypi },
            { "gem", Provider.RubyGems },
            { "golang", Provider.GitHub }
        };
        
        /// <summary>
        /// Der Pakettyp (npm, nuget, maven, pypi, etc.)
        /// </summary>
        public string Type { get; }
        
        /// <summary>
        /// Der Provider für ClearlyDefined (npmjs, nuget, mavencentral, etc.)
        /// </summary>
        public Provider Provider { get; }
        
        /// <summary>
        /// Der Namespace des Pakets (kann null sein, wenn kein Namespace vorhanden ist)
        /// </summary>
        public string? Namespace { get; }
        
        /// <summary>
        /// Der Name des Pakets
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Die Version des Pakets
        /// </summary>
        public string Version { get; }
        
        /// <summary>
        /// Die ursprüngliche PURL
        /// </summary>
        public string OriginalString { get; }
        
        private PackageUrl(string type, Provider provider, string? namespace_, string name, string version, string originalString)
        {
            Type = type;
            Provider = provider;
            Namespace = namespace_;
            Name = name;
            Version = version;
            OriginalString = originalString;
        }
        
        /// <summary>
        /// Versucht, eine PURL zu parsen
        /// </summary>
        public static bool TryParse(string purlString, out PackageUrl? packageUrl)
        {
            packageUrl = null;
            
            try
            {
                packageUrl = Parse(purlString);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Parst eine PURL-Zeichenfolge in ein PackageUrl-Objekt
        /// </summary>
        public static PackageUrl Parse(string purlString)
        {
            if (string.IsNullOrEmpty(purlString))
                throw new ArgumentException("PURL darf nicht null oder leer sein.");
                
            if (!purlString.StartsWith("pkg:"))
                throw new ArgumentException($"PURL muss mit 'pkg:' beginnen: {purlString}");
            
            // Entferne das "pkg:"-Präfix
            var withoutPrefix = purlString.Substring(4);
            
            // Extrahiere den Typ (npm, nuget, maven usw.)
            var segments = withoutPrefix.Split('/');
            if (segments.Length < 2)
                throw new ArgumentException($"PURL hat zu wenige Segmente: {purlString}");
            
            var type = segments[0].ToLowerInvariant();
            
            // Mappe den Typ auf den Provider für ClearlyDefined
            var provider = GetProviderForType(type);
            
            string? namespace_ = null;
            string name;
            string version;
            
            // Fall 1: Der String enthält 2 oder mehr Slashes (hat einen Namespace)
            // Format: pkg:type/namespace/name@version
            if (segments.Length >= 3)
            {
                namespace_ = segments[1];
                
                // Letztes Segment enthält Namen und Version
                var lastSegment = segments[^1];
                var versionSplit = lastSegment.Split('@');
                
                if (versionSplit.Length < 2)
                    throw new ArgumentException($"PURL enthält keine Version: {purlString}");
                
                name = versionSplit[0];
                version = versionSplit[1];
            }
            // Fall 2: Der String enthält nur einen Slash (hat keinen Namespace)
            // Format: pkg:type/name@version
            else
            {
                var nameVersionPart = segments[1];
                var versionSplit = nameVersionPart.Split('@');
                
                if (versionSplit.Length < 2)
                    throw new ArgumentException($"PURL enthält keine Version: {purlString}");
                
                name = versionSplit[0];
                version = versionSplit[1];
            }
            
            return new PackageUrl(type, provider, namespace_, name, version, purlString);
        }
        
        /// <summary>
        /// Mappt einen Pakettyp auf den entsprechenden ClearlyDefined-Provider
        /// </summary>
        private static Provider GetProviderForType(string type)
        {
            if (TypeToProviderMap.TryGetValue(type, out var provider))
            {
                return provider;
            }
            
            throw new ArgumentException($"Unbekannter Pakettyp: {type}");
        }
        
        public override string ToString() => OriginalString;
    }
}
