using CdxEnrich.ClearlyDefined;
using PackageUrl;

namespace CdxEnrich.Tests.ClearlyDefined
{
    /// <summary>
    /// Integration tests that call the real ClearlyDefined API.
    /// These tests are marked as Explicit to avoid running in CI by default.
    /// Run manually with: dotnet test --filter "Category=Integration"
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    [Explicit("Calls real external API - run manually to verify API format")]
    public class ClearlyDefinedClientIntegrationTests
    {
        private ClearlyDefinedClient _client = null!;

        [SetUp]
        public void Setup()
        {
            var httpClientFactory = new TestHttpClientFactory();
            _client = new ClearlyDefinedClient(httpClientFactory);
        }

        [Test]
        public async Task GetLicenseData_ForNpmPackage_ReturnsExpectedFormat()
        {
            // Arrange - lodash is a well-known npm package with stable license data
            var packageUrl = new PackageURL("pkg:npm/lodash@4.17.21");

            // Act
            var result = await _client.GetClearlyDefinedLicensedDataAsync(packageUrl, Provider.Npmjs);

            // Assert
            Assert.That(result, Is.Not.Null, "Should receive license data from ClearlyDefined");
            Assert.That(result.Declared, Is.Not.Null.And.Not.Empty, "Should have declared license");
            Assert.That(result.Facets, Is.Not.Null, "Should have facets data");
            Assert.That(result.Facets.Core, Is.Not.Null, "Should have core facet");
            Assert.That(result.Facets.Core.Discovered, Is.Not.Null, "Should have discovered data");
            
            // Verify the structure - don't assert specific license value as it may change
            Assert.That(result.Declared, Does.Contain("MIT"), "lodash@4.17.21 should include MIT license");
            
            Console.WriteLine($"Declared: {result.Declared}");
            Console.WriteLine($"Discovered expressions: {result.Facets.Core.Discovered.Expressions?.Count ?? 0}");
        }

        [Test]
        public async Task GetLicenseData_ForNugetPackage_ReturnsExpectedFormat()
        {
            // Arrange - Newtonsoft.Json is a well-known NuGet package
            var packageUrl = new PackageURL("pkg:nuget/Newtonsoft.Json@13.0.1");

            // Act
            var result = await _client.GetClearlyDefinedLicensedDataAsync(packageUrl, Provider.Nuget);

            // Assert
            Assert.That(result, Is.Not.Null, "Should receive license data from ClearlyDefined");
            Assert.That(result.Declared, Is.Not.Null.And.Not.Empty, "Should have declared license");
            Assert.That(result.Facets, Is.Not.Null, "Should have facets data");
            
            // Verify the declared license is MIT (known for Newtonsoft.Json)
            Assert.That(result.Declared, Is.EqualTo("MIT"), "Newtonsoft.Json@13.0.1 should have MIT license");
            
            Console.WriteLine($"Declared: {result.Declared}");
            Console.WriteLine($"Discovered expressions: {result.Facets.Core.Discovered.Expressions?.Count ?? 0}");
        }

        [Test]
        public async Task LicenseResolver_WithRealApiData_ResolvesMitLicense()
        {
            // Arrange
            var packageUrl = new PackageURL("pkg:npm/express@4.18.2");

            // Act
            var licensedData = await _client.GetClearlyDefinedLicensedDataAsync(packageUrl, Provider.Npmjs);
            Assert.That(licensedData, Is.Not.Null, "Should receive data from API");
            
            var licenseChoice = LicenseResolver.Resolve(packageUrl, licensedData);

            // Assert
            Assert.That(licenseChoice, Is.Not.Null, "Should resolve license");
            Assert.That(licenseChoice.License, Is.Not.Null, "Should have License object");
            Assert.That(licenseChoice.License.Id, Is.EqualTo("MIT"), "express@4.18.2 should resolve to MIT");
            Assert.That(licenseChoice.Expression, Is.Null, "Should use License.Id not Expression for simple license");
            
            Console.WriteLine($"Resolved: {licenseChoice.License.Id}");
        }

        /// <summary>
        /// Simple HttpClientFactory for testing that creates a real HttpClient
        /// </summary>
        private class TestHttpClientFactory : IHttpClientFactory
        {
            public HttpClient CreateClient(string name)
            {
                var client = new HttpClient
                {
                    BaseAddress = ClearlyDefinedClient.ClearlyDefinedApiBaseAddress,
                    Timeout = TimeSpan.FromSeconds(60)
                };
                return client;
            }
        }
    }
}
