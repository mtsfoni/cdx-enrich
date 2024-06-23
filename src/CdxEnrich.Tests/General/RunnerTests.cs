using CdxEnrich;

namespace CdxEnrich.Tests.General
{
    public class BomEnricher_Test
    {
        [SetUp]
        public void Setup()
        {
        }


        [Test]
        public Task EnrichTest(
            [Values("general.json", "general.xml")]
            string bomName,
            [Values("general.yaml", "empty.yaml", "actionsEmpty.yaml", "actionsMissing1.yaml", "actionsMissing2.yaml")]
            string configName,
            [Values(CycloneDXFormat.JSON, CycloneDXFormat.XML)]
            CycloneDXFormat outputFormat)
        {
            string configPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "../../..", "General/testcases", configName));
            string bomPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "../../..", "General/testcases", bomName));

            var extension = Path.GetExtension(bomPath);
            CycloneDXFormat inputFormat = extension.Equals(".json", StringComparison.CurrentCultureIgnoreCase) ? CycloneDXFormat.JSON : CycloneDXFormat.XML;

            string config = File.ReadAllText(configPath);
            string bom = File.ReadAllText(bomPath);




            var result = Runner.Enrich(bom, inputFormat, config, outputFormat);

            var settings = new VerifySettings();
            settings.UseDirectory("testcases/snapshots");
            settings.AddExtraSettings(
                _ => _.ContractResolver = new VerifyContractResolver());
            return Verify(result, settings);
        }
    }
}