using CycloneDX.Models;
using CycloneDX;

namespace CdxEnrich.Tests
{
    public static class BomValidationHelper
    {
        public static bool IsValidBom(Bom bom, SpecificationVersion specVersion, out List<string> problems)
        {
            var json = CycloneDX.Json.Serializer.Serialize(bom);
            var xml = CycloneDX.Xml.Serializer.Serialize(bom);

            var validationResultJson = CycloneDX.Json.Validator.Validate(json, specVersion);
            var validationResultXml = CycloneDX.Xml.Validator.Validate(xml, specVersion);

            problems = [.. validationResultJson.Messages, .. validationResultXml.Messages];

            return validationResultJson.Valid && validationResultXml.Valid;
        }

        public static void AssertValidBom(Bom bom, SpecificationVersion specVersion, string context = "")
        {
            var isValid = IsValidBom(bom, specVersion, out var problems);
            
            if (!isValid)
            {
                var errorMessage = string.IsNullOrEmpty(context) 
                    ? "BOM validation failed"
                    : $"BOM validation failed for {context}";
                
                errorMessage += $":\n  - {string.Join("\n  - ", problems)}";
                
                Assert.Fail(errorMessage);
            }
        }
    }
}
