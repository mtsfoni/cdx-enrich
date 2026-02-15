using CdxEnrich;
using CdxEnrich.FunctionalHelpers;
using CdxEnrich.Serialization;
using CycloneDX;
using CdxEnrich.Actions;
using Microsoft.Extensions.DependencyInjection;

namespace CdxEnrich.Tests.General
{
    public class BomEnricher_Test
    {
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _fixture = new Fixture();
        }

        private class Fixture
        {
            private readonly ServiceProvider _serviceProvider;

            public Fixture()
            {
                var serviceCollection = new ServiceCollection();
                Program.ConfigureServices(serviceCollection);

                this._serviceProvider = serviceCollection.BuildServiceProvider();
            }

            public Runner CreateSut()
            {
                var replaceActions = this._serviceProvider.GetRequiredService<IEnumerable<IReplaceAction>>();
                return new Runner(replaceActions);
            }
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

<<<<<<< HEAD
            var result = Runner.Enrich(bom, inputFormat, config, outputFormat);
=======


            var runner = this._fixture.CreateSut();
            var result = runner.Enrich(bom, inputFormat, config, outputFormat);
>>>>>>> feature/3-Replace-SBOM-Licenses-with-ClearlyDefined-Discovered-Licenses

            // Validate the result is a valid BOM if successful
            if (result is Ok<string> successResult)
            {
                var bomResult = BomSerialization.DeserializeBom(successResult.Data, outputFormat);
                if (bomResult is Ok<CycloneDX.Models.Bom> bomOk)
                {
                    BomValidationHelper.AssertValidBom(bomOk.Data, SpecificationVersion.v1_5, $"Enrich with {bomName} + {configName} → {outputFormat}");
                }
            }

            var settings = new VerifySettings();
            settings.UseDirectory("testcases/snapshots");
            settings.AddExtraSettings(
                _ => _.ContractResolver = new VerifyContractResolver());
            return Verify(result, settings);
        }
    }
}