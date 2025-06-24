using System.Text.Json.Serialization;

namespace CdxEnrich.ClearlyDefined
{
    public class ClearlyDefinedResponse
    {
        [JsonPropertyName("licensed")] public required LicensedData Licensed { get; init; }

        public class LicensedData
        {
            [JsonPropertyName("facets")] public Facets? Facets { get; init; }
            [JsonPropertyName("declared")] public string? Declared { get; init; }
            
            [JsonPropertyName("toolScore")] public ToolScore ToolScore { get; init; }
        }

        public class ToolScore
        {
            [JsonPropertyName("total")] public required int Total { get; init; }
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