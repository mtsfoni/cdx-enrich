using CdxEnrich.Actions;
using CdxEnrich.ClearlyDefined;
using CdxEnrich.Config;
using CdxEnrich.FunctionalHelpers;
using CdxEnrich.Logging;
using CdxEnrich.Serialization;

namespace CdxEnrich.Tests.Actions.ReplaceLicenseByClearlyDefined
{
    internal class ReplaceLicenseByClearlyDefinedTest
    {
        private readonly Fixture _fixture = new();

        private class Fixture
        {
            public IReplaceAction CreateReplaceAction()
            {
                return new CdxEnrich.Actions.ReplaceLicenseByClearlyDefined(
                    new ConsoleLogger<CdxEnrich.Actions.ReplaceLicenseByClearlyDefined>(),
                    new ClearlyDefinedClient(new HttpClient { Timeout = TimeSpan.FromSeconds(60) }, logger: new ConsoleLogger<ClearlyDefinedClient>()),
                    new LicenseResolver(new ConsoleLogger<LicenseResolver>())
                    );
            }
        }

        private static string[] GetConfigs(string startingWith)
        {
            string testFilesPath = Path.Combine(Environment.CurrentDirectory, "../../..", "Actions/ReplaceLicenseByClearlyDefined/testcases/configs");
            var files = Directory.GetFiles(testFilesPath).Where(s => Path.GetFileNameWithoutExtension(s).StartsWith(startingWith)).ToArray();
            if (files.Length == 0)
            {
                throw new Exception("No Testfiles found!");
            }
            return files;
        }
        
        private static CycloneDXFormat GetCycloneDxFormat(string bomPath)
        {
            var extension = Path.GetExtension(bomPath);
            CycloneDXFormat inputFormat = extension.Equals(".json", StringComparison.CurrentCultureIgnoreCase) ? CycloneDXFormat.JSON : CycloneDXFormat.XML;
            return inputFormat;
        }
        
        private static IEnumerable<object[]> GetInputPairs(string startingWith)
        {
            bool returnedAtLeastOneSet = false;

            foreach (string filePath in GetConfigs(startingWith))
            {
                string testFilesPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "../../..", "Actions/ReplaceLicenseByClearlyDefined/testcases/boms"));

                foreach (string bomPath in Directory.GetFiles(testFilesPath))
                {
                    yield return new object[] { Path.GetFullPath(filePath), bomPath };
                    returnedAtLeastOneSet = true;
                }
            }
            
            if (!returnedAtLeastOneSet)
            {
                throw new Exception("No Testfiles found!");
            }
        }

        [Test]
        [TestCaseSource(nameof(GetConfigs), new object[] { "" })]
        public void CanParseConfig(string configPath)
        {
            var configContent = File.ReadAllText(configPath);
            var parseConfigResult = ConfigLoader.ParseConfig(configContent);

            Assert.That(parseConfigResult is not Failure);
        }

        [Test]
        [TestCaseSource(nameof(GetInputPairs), new object[] { "invalid" })]
        public void InvalidBomAndConfigCombinationsReturnError(string configPath, string bomPath)
        {
            var inputFormat = GetCycloneDxFormat(bomPath);
            string bomContent = File.ReadAllText(bomPath);
            var replaceAction = this._fixture.CreateReplaceAction();

            var checkConfigResult =
                Runner.CombineBomAndConfig(BomSerialization.DeserializeBom(bomContent, inputFormat),
                        ConfigLoader.ParseConfig(File.ReadAllText(configPath))
                            .Bind(replaceAction.CheckConfig))
                    .Bind(replaceAction.CheckBomAndConfigCombination);

            Assert.That(checkConfigResult is Failure);
        }

        [Test]
        [TestCaseSource(nameof(GetConfigs), new object[] { "valid" })]
        public void ValidConfigsReturnSuccess(string configPath)
        {
            var configContent = File.ReadAllText(configPath);
            var replaceAction = this._fixture.CreateReplaceAction();
            var checkConfigResult = ConfigLoader.ParseConfig(configContent)
                .Bind(replaceAction.CheckConfig);

            Assert.That(checkConfigResult is Success);
        }

        [Test]
        [TestCaseSource(nameof(GetInputPairs), new object[] { "valid" })]
        public Task ExecuteActionCreateCorrectResults(string configPath, string bomPath)
        {
            var inputFormat = GetCycloneDxFormat(bomPath);
            string bomContent = File.ReadAllText(bomPath);
            var replaceAction = this._fixture.CreateReplaceAction();

            var executionResult =
                Runner.CombineBomAndConfig(BomSerialization.DeserializeBom(bomContent, inputFormat),
                    ConfigLoader.ParseConfig(File.ReadAllText(configPath))
                    .Bind(replaceAction.CheckConfig))
                    .Bind(replaceAction.CheckBomAndConfigCombination)
                .Map(replaceAction.Execute);

            Assert.That(executionResult is Success);

            var settings = new VerifySettings();
            settings.UseDirectory("testcases/snapshots");
            settings.UseFileName($"Config_{Path.GetFileName(configPath)}_Bom_{Path.GetFileName(bomPath)}");

            settings.AddExtraSettings(
                _ => _.ContractResolver = new VerifyContractResolver());
            return Verify(executionResult.Data.Bom, settings);
        }
    }
}
