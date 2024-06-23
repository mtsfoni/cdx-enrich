using CdxEnrich;
using CdxEnrich.FunctionalHelpers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CdxEnrich.Config
{
    public static class ConfigLoader
    {
        public static Result<ConfigRoot> ParseConfig(string configContent)
        {
            try
            {
                if (string.IsNullOrEmpty(configContent))
                {
                    return YamlDeserializationError.Create<ConfigRoot>("Provided config-file is empty.");
                }
                else
                {
                    var deserializer = new DeserializerBuilder()
                        .WithNamingConvention(PascalCaseNamingConvention.Instance)
                        .Build();

                    var config = deserializer.Deserialize<ConfigRoot>(configContent);

                    return new Ok<ConfigRoot>(config);
                }


            }
            catch (Exception ex)
            {
                return YamlDeserializationError.Create<ConfigRoot>(ex.Message);
            }
        }
    }
}
