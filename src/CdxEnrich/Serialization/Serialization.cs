using CdxEnrich;
using CdxEnrich.Config;
using CdxEnrich.FunctionalHelpers;
using CycloneDX.Models;

namespace CdxEnrich.Serialization
{
    public static class BomSerialization
    {
        public static Result<Bom> DeserializeBom(string fileText, CycloneDXFormat inputFormat) =>
            inputFormat switch
            {
                CycloneDXFormat.JSON => ParseJSON(fileText),
                CycloneDXFormat.XML => ParseXML(fileText),
                _ => InvalidFileFormatError.Create<Bom>()
            };

        private static Result<Bom> ParseJSON(string text)
        {
            try
            {
                return new Ok<Bom>(CycloneDX.Json.Serializer.Deserialize(text));
            }
            catch (Exception)
            {
                return BomDeserializationError.Create<Bom>("JSON");
            }
        }
        private static Result<Bom> ParseXML(string text)
        {
            try
            {
                return new Ok<Bom>(CycloneDX.Xml.Serializer.Deserialize(text));
            }
            catch (Exception)
            {
                return BomDeserializationError.Create<Bom>("XML");
            }
        }

        public static Result<string> SerializeBom(InputTuple inputs, CycloneDXFormat format)
        {
            try
            {
                return
                    new Ok<string>(
                    format == CycloneDXFormat.JSON ? CycloneDX.Json.Serializer.Serialize(inputs.Bom) :
                    CycloneDX.Xml.Serializer.Serialize(inputs.Bom));
            }
            catch (Exception ex)
            {
                return BomSerializationError.Create<string>(ex.Message);
            }
        }
    }
}