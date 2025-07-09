using CdxEnrich;
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

        public class Fixture
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
                return new Runner();
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



            var runner = this._fixture.CreateSut();
            var result = runner.Enrich(bom, inputFormat, config, outputFormat);

            var settings = new VerifySettings();
            settings.UseDirectory("testcases/snapshots");
            settings.AddExtraSettings(
                _ => _.ContractResolver = new VerifyContractResolver());
            return Verify(result, settings);
        }
    }
}