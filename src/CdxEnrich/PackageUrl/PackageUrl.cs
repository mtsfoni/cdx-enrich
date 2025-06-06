using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace CdxEnrich.PackageUrl
{
    /// <summary>
    /// Repräsentiert eine Package URL (PURL) nach der Spezifikation
    /// https://github.com/package-url/purl-spec
    /// </summary>
    [Serializable]
    public sealed class PackageUrl
    {
        /// <summary>
        /// Die URL-Codierung von /.
        /// </summary>
        private const string EncodedSlash = "%2F";
        private const string EncodedColon = "%3A";

        private static readonly Regex s_typePattern = new Regex("^[a-zA-Z][a-zA-Z0-9.+-]+$", RegexOptions.Compiled);

        /// <summary>
        /// Die PackageURL-Schema-Konstante.
        /// </summary>
        public string Scheme { get; private set; } = "pkg";

        /// <summary>
        /// Der Pakettyp (npm, nuget, maven, pypi, etc.)
        /// </summary>
        public string Type { get; private set; }

        /// <summary>
        /// Der Namespace des Pakets (kann null sein)
        /// </summary>
        public string Namespace { get; private set; }

        /// <summary>
        /// Der Name des Pakets
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Die Version des Pakets
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        /// Zusätzliche qualifizierende Daten für ein Paket
        /// </summary>
        public SortedDictionary<string, string> Qualifiers { get; private set; }

        /// <summary>
        /// Zusätzlicher Subpfad innerhalb eines Pakets
        /// </summary>
        public string Subpath { get; private set; }

        /// <summary>
        /// Konstruiert ein neues PackageUrl-Objekt durch Parsen der angegebenen Zeichenfolge.
        /// </summary>
        /// <param name="purl">Eine gültige Package-URL-Zeichenfolge zum Parsen.</param>
        /// <exception cref="MalformedPackageUrlException">Wird geworfen, wenn das Parsen fehlschlägt.</exception>
        public PackageUrl(string purl)
        {
            Parse(purl);
        }

        /// <summary>
        /// Konstruiert ein neues PackageUrl-Objekt durch Angabe der erforderlichen Parameter.
        /// </summary>
        /// <param name="type">Typ des Pakets (z.B. nuget, npm, gem, etc.).</param>
        /// <param name="name">Name des Pakets.</param>
        /// <exception cref="MalformedPackageUrlException">Wird geworfen, wenn ungültige Parameter übergeben werden.</exception>
        public PackageUrl(string type, string name) : this(type, null, name, null, null, null)
        {
        }

        /// <summary>
        /// Konstruiert ein neues PackageUrl-Objekt.
        /// </summary>
        /// <param name="type">Typ des Pakets (z.B. nuget, npm, gem, etc.).</param>
        /// <param name="namespace">Namespace des Pakets (z.B. Gruppe, Eigentümer, Organisation).</param>
        /// <param name="name">Name des Pakets.</param>
        /// <param name="version">Version des Pakets.</param>
        /// <param name="qualifiers">Dictionary mit Key/Value-Qualifikatoren.</param>
        /// <param name="subpath">Der Subpfad.</param>
        /// <exception cref="MalformedPackageUrlException">Wird geworfen, wenn ungültige Parameter übergeben werden.</exception>
        public PackageUrl(string type, string @namespace, string name, string version, SortedDictionary<string, string> qualifiers, string subpath)
        {
            Type = ValidateType(type);
            Namespace = ValidateNamespace(@namespace);
            Name = ValidateName(name);
            Version = version;
            Qualifiers = qualifiers;
            Subpath = ValidateSubpath(subpath);
        }

        /// <summary>
        /// Versucht, eine PURL zu parsen
        /// </summary>
        public static bool TryParse(string purlString, out PackageUrl packageUrl)
        {
            packageUrl = null;
            
            try
            {
                packageUrl = new PackageUrl(purlString);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gibt eine kanonisierte Darstellung der PURL zurück.
        /// </summary>
        public override string ToString()
        {
            var purl = new StringBuilder();
            purl.Append(Scheme).Append(':');
            if (Type != null)
            {
                purl.Append(Type);
            }
            purl.Append('/');
            if (Namespace != null)
            {
                string encodedNamespace = WebUtility.UrlEncode(Namespace).Replace(EncodedSlash, "/");
                purl.Append(encodedNamespace);
                purl.Append('/');
            }
            if (Name != null)
            {
                string encodedName = WebUtility.UrlEncode(Name).Replace(EncodedColon, ":");
                purl.Append(encodedName);
            }
            if (Version != null)
            {
                string encodedVersion = WebUtility.UrlEncode(Version).Replace(EncodedColon, ":");
                purl.Append('@').Append(encodedVersion);
            }
            if (Qualifiers != null && Qualifiers.Count > 0)
            {
                purl.Append("?");
                foreach (var pair in Qualifiers)
                {
                    string encodedValue = WebUtility.UrlEncode(pair.Value).Replace(EncodedSlash, "/");
                    purl.Append(pair.Key.ToLower());
                    purl.Append('=');
                    purl.Append(encodedValue);
                    purl.Append('&');
                }
                purl.Remove(purl.Length - 1, 1);
            }
            if (Subpath != null)
            {
                string encodedSubpath = WebUtility.UrlEncode(Subpath).Replace(EncodedSlash, "/").Replace(EncodedColon, ":");
                purl.Append("#").Append(encodedSubpath);
            }
            return purl.ToString();
        }

        private void Parse(string purl)
        {
            if (purl == null || string.IsNullOrWhiteSpace(purl))
            {
                throw new Exception("Invalid purl: Contains an empty or null value");
            }

            Uri uri;
            try
            {
                uri = new Uri(purl);
            }
            catch (UriFormatException e)
            {
                throw new Exception("Invalid purl: " + e.Message);
            }

            // Überprüfe, dass keine dieser Teile geparst werden. Wenn ja, ist es eine ungültige PURL.
            if (!string.IsNullOrEmpty(uri.UserInfo) || uri.Port != -1)
            {
                throw new Exception("Invalid purl: Contains parts not supported by the purl spec");
            }

            if (uri.Scheme != "pkg")
            {
                throw new Exception("The PackageURL scheme is invalid");
            }

            // Das ist die PURL (minus das Schema), die geparst werden muss.
            string remainder = purl.Substring(4);

            if (remainder.Contains("#"))
            { // Subpfad ist optional - überprüfe auf Existenz
                int index = remainder.LastIndexOf("#");
                Subpath = ValidateSubpath(WebUtility.UrlDecode(remainder.Substring(index + 1)));
                remainder = remainder.Substring(0, index);
            }

            if (remainder.Contains("?"))
            { // Qualifikatoren sind optional - überprüfe auf Existenz
                int index = remainder.LastIndexOf("?");
                Qualifiers = ValidateQualifiers(remainder.Substring(index + 1));
                remainder = remainder.Substring(0, index);
            }

            if (remainder.Contains("@"))
            { // Version ist optional - überprüfe auf Existenz
                int index = remainder.LastIndexOf("@");
                Version = WebUtility.UrlDecode(remainder.Substring(index + 1));
                remainder = remainder.Substring(0, index);
            }

            // Der 'remainder' sollte jetzt aus dem Typ, einem optionalen Namespace und dem Namen bestehen

            // Entferne null oder mehr '/' ('Typ')
            remainder = remainder.Trim('/');

            string[] firstPartArray = remainder.Split('/');
            if (firstPartArray.Length < 2)
            { // Das Array muss mindestens einen 'Typ' und einen 'Namen' enthalten
                throw new Exception("Invalid purl: Does not contain a minimum of a 'type' and a 'name'");
            }

            Type = ValidateType(firstPartArray[0]);
            Name = ValidateName(WebUtility.UrlDecode(firstPartArray[firstPartArray.Length - 1]));

            // Teste auf Namespaces
            if (firstPartArray.Length > 2)
            {
                string @namespace = "";
                int i;
                for (i = 1; i < firstPartArray.Length - 2; ++i)
                {
                    @namespace += firstPartArray[i] + '/';
                }
                @namespace += firstPartArray[i];

                Namespace = ValidateNamespace(WebUtility.UrlDecode(@namespace));
            }
        }

        private static string ValidateType(string type)
        {
            if (type == null || !s_typePattern.IsMatch(type))
            {
                throw new Exception("The PackageURL type specified is invalid");
            }
            return type.ToLower();
        }

        private string ValidateNamespace(string @namespace)
        {
            if (@namespace == null)
            {
                return null;
            }
            return Type switch
            {
                "bitbucket" or "github" or "pypi" or "gitlab" => @namespace.ToLower(),
                _ => @namespace
            };
        }

        private string ValidateName(string name)
        {
            if (name == null)
            {
                throw new Exception("The PackageURL name specified is invalid");
            }
            return Type switch
            {
                "bitbucket" or "github" or "gitlab" => name.ToLower(),
                "pypi" => name.Replace('_', '-').ToLower(),
                _ => name
            };
        }

        private static SortedDictionary<string, string> ValidateQualifiers(string qualifiers)
        {
            var list = new SortedDictionary<string, string>();
            string[] pairs = qualifiers.Split('&');
            foreach (var pair in pairs)
            {
                if (pair.Contains("="))
                {
                    string[] kvpair = pair.Split('=');
                    list.Add(kvpair[0], WebUtility.UrlDecode(kvpair[1]));
                }
            }
            return list;
        }

        private static string ValidateSubpath(string subpath) => subpath?.Trim('/'); // Führende und nachfolgende Schrägstriche müssen immer entfernt werden
    }
}
