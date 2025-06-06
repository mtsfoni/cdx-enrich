using System.Text.Json.Serialization;

namespace CdxEnrich.ClearlyDefined
{
    public class ClearlyDefinedResponse
    {
        [JsonPropertyName("licensed")] public required LicensedData Licensed { get; init; }

        public class LicensedData
        {
            [JsonPropertyName("facets")] public required Facets Facets { get; init; }
        }

        public class Facets
        {
            [JsonPropertyName("core")] public required Core Core { get; init; }
        }

        public class Core
        {
            [JsonPropertyName("discovered")] public required Discovered Discovered { get; init; }
        }

        public class Discovered
        {
            [JsonPropertyName("expressions")] public List<string>? Expressions { get; init; }
        }
    }
}