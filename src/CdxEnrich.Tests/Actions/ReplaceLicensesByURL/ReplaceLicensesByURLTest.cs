using CdxEnrich;
using CdxEnrich.Config;
using CdxEnrich.FunctionalHelpers;
using CdxEnrich.Serialization;
using CdxEnrich.Actions;
using CycloneDX;
using VerifyNUnit;

namespace CdxEnrich.Tests.Actions
{
    internal class ReplaceLicensesByURLTest
    {
        private static string[] GetConfigs(string startingWith)
        {
            string testFilesPath = Path.Combine(Environment.CurrentDirectory, "../../..", "Actions/ReplaceLicensesByURL/testcases/configs");
            var files = Directory.GetFiles(testFilesPath).Where(s => Path.GetFileNameWithoutExtension(s).StartsWith(startingWith)).ToArray();
            if (files.Length == 0)
            {
                throw new Exception("No Testfiles found!");
            }
            return files;

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
        [TestCaseSource(nameof(GetConfigs), new object[] { "invalid" })]
        public void InvalidConfigsReturnError(string configPath)
        {
            var configContent = File.ReadAllText(configPath);
            var checkConfigResult = ConfigLoader.ParseConfig(configContent)
                .Bind(CdxEnrich.Actions.ReplaceLicensesByUrl.CheckConfig);

            Assert.That(checkConfigResult is Failure);
        }

        [Test]
        [TestCaseSource(nameof(GetConfigs), new object[] { "valid" })]
        public void ValidConfigsReturnSuccess(string configPath)
        {
            var configContent = File.ReadAllText(configPath);
            var checkConfigResult = ConfigLoader.ParseConfig(configContent)
                .Bind(CdxEnrich.Actions.ReplaceLicensesByUrl.CheckConfig);

            Assert.That(checkConfigResult is Success);
        }

        private static IEnumerable<object[]> GetInputPairs()
        {
            bool returnedAtLeastOneSet = false;

            foreach (string filePath in GetConfigs("valid"))
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string testFilesPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "../../..", "Actions/ReplaceLicensesByURL/testcases/boms"));

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
        [TestCaseSource(nameof(GetInputPairs))]
        public Task ExecuteActionCreateCorrectResults(string configPath, string bomPath)
        {
            var extension = Path.GetExtension(bomPath);
            CycloneDXFormat inputFormat = extension.Equals(".json", StringComparison.CurrentCultureIgnoreCase) ? CycloneDXFormat.JSON : CycloneDXFormat.XML;
            string bomContent = File.ReadAllText(bomPath);
            
            var executionResult =
                Runner.CombineBomAndConfig(BomSerialization.DeserializeBom(bomContent, inputFormat),
                    ConfigLoader.ParseConfig(File.ReadAllText(configPath))
                    .Bind(CdxEnrich.Actions.ReplaceLicensesByUrl.CheckConfig))
                .Map(CdxEnrich.Actions.ReplaceLicensesByUrl.Execute);

            Assert.That(executionResult is Success);

            // Validate the output BOM against CycloneDX schema
            BomValidationHelper.AssertValidBom(
                executionResult.Data.Bom,
                SpecificationVersion.v1_5,
                $"ReplaceLicensesByUrl with {Path.GetFileName(configPath)} and {Path.GetFileName(bomPath)}");

            var settings = new VerifySettings();
            settings.UseDirectory("testcases/snapshots");
            settings.UseFileName($"Config_{Path.GetFileName(configPath)}_Bom_{Path.GetFileName(bomPath)}");

            settings.AddExtraSettings(
                _ => _.ContractResolver = new VerifyContractResolver());
            return Verify(executionResult.Data.Bom, settings);
        }
    }
}