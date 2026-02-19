using System.Net;
using CdxEnrich.ClearlyDefined;
using PackageUrl;

namespace CdxEnrich.Tests.ClearlyDefined
{
    [TestFixture]
    public partial class ClearlyDefinedClientTests
    {
        [SetUp]
        public void Setup()
        {
            this._fixture = new ClearlyDefinedClientFixture();
        }

        [TearDown]
        public void TearDown()
        {
            this._fixture.Dispose();
        }

        private ClearlyDefinedClientFixture _fixture;

        [Test]
        public async Task GetClearlyDefinedLicensesAsync_WhenApiReturnsSuccess_ReturnsLicenses()
        {
            // Arrange
            var packageUrl = new PackageURL("npm", null, "lodash", "4.17.21", null, null);
            var provider = Provider.Npmjs;
            this._fixture.SetupSuccessResponse("MIT", []);

            // Act
            var result = await this._fixture.Client.GetClearlyDefinedLicensedDataAsync(packageUrl, provider);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Declared, Is.EqualTo("MIT"));

            this._fixture.VerifyRequestUri("npm/npmjs/-/lodash/4.17.21");
        }

        [Test]
        public async Task GetClearlyDefinedLicensesAsync_WithNamespace_ConstructsCorrectUrl()
        {
            // Arrange
            var packageUrl = new PackageURL("maven", "org.apache.commons", "commons-lang3", "3.12.0", null, null);
            var provider = Provider.MavenCentral;
            this._fixture.SetupSuccessResponse("Apache-2.0", []);

            // Act
            var result = await this._fixture.Client.GetClearlyDefinedLicensedDataAsync(packageUrl, provider);

            // Assert
            Assert.That(result, Is.Not.Null);

            this._fixture.VerifyRequestUri("maven/mavencentral/org.apache.commons/commons-lang3/3.12.0");
        }

        [Test]
        public async Task GetClearlyDefinedLicensesAsync_WhenApiReturnsError_ReturnsNull()
        {
            // Arrange
            var packageUrl = new PackageURL("npm", null, "nonexistent", "1.0.0", null, null);
            var provider = Provider.Npmjs;
            this._fixture.SetupErrorResponse(HttpStatusCode.NotFound);

            // Act
            var result = await this._fixture.Client.GetClearlyDefinedLicensedDataAsync(packageUrl, provider);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetClearlyDefinedLicensesAsync_WhenApiThrowsException_ReturnsNull()
        {
            // Arrange
            var packageUrl = new PackageURL("npm", null, "lodash", "4.17.21", null, null);
            var provider = Provider.Npmjs;
            this._fixture.SetupExceptionResponse(new HttpRequestException("Netzwerkfehler"));

            // Act
            var result = await this._fixture.Client.GetClearlyDefinedLicensedDataAsync(packageUrl, provider);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetClearlyDefinedLicensesAsync_WhenRateLimited_RetriesAndSucceeds()
        {
            // Arrange
            var packageUrl = new PackageURL("npm", null, "lodash", "4.17.21", null, null);
            var provider = Provider.Npmjs;
            this._fixture.SetupRateLimitThenSuccessResponse("MIT", []);

            // Act
            var result = await this._fixture.Client.GetClearlyDefinedLicensedDataAsync(packageUrl, provider);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Declared, Is.EqualTo("MIT"));

            Assert.That(this._fixture.HttpHandler.RequestsReceived.Count, Is.EqualTo(2));

        }

        // Helper class for HTTP tests
        private class TestHttpMessageHandler : HttpMessageHandler, IDisposable
        {
            private int _sequenceIndex;
            public List<HttpRequestMessage> RequestsReceived { get; } = new();
            public HttpResponseMessage ResponseToReturn { get; set; } = new(HttpStatusCode.OK);
            public List<HttpResponseMessage> ResponseSequence { get; } = new();
            public Exception? ExceptionToThrow { get; set; }

            public new void Dispose()
            {
                base.Dispose();
                foreach (var request in this.RequestsReceived)
                {
                    request.Dispose();
                }

                this.ResponseToReturn.Dispose();
                foreach (var response in this.ResponseSequence)
                {
                    response.Dispose();
                }
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                this.RequestsReceived.Add(request);

                if (this.ExceptionToThrow != null)
                {
                    throw this.ExceptionToThrow;
                }

                if (this.ResponseSequence.Count > 0)
                {
                    if (this._sequenceIndex < this.ResponseSequence.Count)
                    {
                        return Task.FromResult(this.ResponseSequence[this._sequenceIndex++]);
                    }

                    return Task.FromResult(this.ResponseSequence.Last());
                }

                return Task.FromResult(this.ResponseToReturn);
            }
        }
    }
}