using System.Net;
using System.Text.Json;
using CdxEnrich.ClearlyDefined;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PackageUrl;

namespace CdxEnrich.Tests.ClearlyDefined
{
    [TestFixture]
    public class ClearlyDefinedClientTests
    {
        [SetUp]
        public void Setup()
        {
            this._logger = Substitute.For<ILogger<ClearlyDefinedClient>>();
            this._testHandler = new TestHttpMessageHandler();
            this._httpClient = new HttpClient(this._testHandler);
            this._client = new ClearlyDefinedClient(this._httpClient, this._logger);
        }

        [TearDown]
        public void TearDown()
        {
            this._httpClient.Dispose();
            this._testHandler.Dispose();
        }

        private ILogger<ClearlyDefinedClient> _logger;
        private HttpClient _httpClient;
        private ClearlyDefinedClient _client;
        private TestHttpMessageHandler _testHandler;

        [Test]
        public async Task GetClearlyDefinedLicensesAsync_WhenApiReturnsSuccess_ReturnsLicenses()
        {
            // Arrange
            var packageUrl = new PackageURL("npm", null, "lodash", "4.17.21", null, null);
            var provider = Provider.Npmjs;

            var responseContent = new ClearlyDefinedResponse
            {
                Licensed = new ClearlyDefinedResponse.LicensedData
                {
                    Facets = new ClearlyDefinedResponse.Facets
                    {
                        Core = new ClearlyDefinedResponse.Core
                        {
                            Discovered = new ClearlyDefinedResponse.Discovered
                            {
                                Expressions = ["MIT"]
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(responseContent);
            this._testHandler.ResponseToReturn = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            };

            // Act
            var result = await this._client.GetClearlyDefinedLicensesAsync(packageUrl, provider);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0], Is.EqualTo("MIT"));

            Assert.That(this._testHandler.RequestsReceived.Count, Is.EqualTo(1));
            Assert.That(this._testHandler.RequestsReceived[0].Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(this._testHandler.RequestsReceived[0].RequestUri, Is.Not.Null);
            Assert.That(this._testHandler.RequestsReceived[0].RequestUri.ToString(),
                Does.Contain("npm/npmjs/-/lodash/4.17.21"));
        }

        [Test]
        public async Task GetClearlyDefinedLicensesAsync_WithNamespace_ConstructsCorrectUrl()
        {
            // Arrange
            var packageUrl = new PackageURL("maven", "org.apache.commons", "commons-lang3", "3.12.0", null, null);
            var provider = Provider.MavenCentral;

            var responseContent = new ClearlyDefinedResponse
            {
                Licensed = new ClearlyDefinedResponse.LicensedData
                {
                    Facets = new ClearlyDefinedResponse.Facets
                    {
                        Core = new ClearlyDefinedResponse.Core
                        {
                            Discovered = new ClearlyDefinedResponse.Discovered
                            {
                                Expressions = ["Apache-2.0"]
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(responseContent);
            this._testHandler.ResponseToReturn = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            };

            // Act
            var result = await this._client.GetClearlyDefinedLicensesAsync(packageUrl, provider);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));

            Assert.That(this._testHandler.RequestsReceived.Count, Is.EqualTo(1));
            Assert.That(this._testHandler.RequestsReceived[0].Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(this._testHandler.RequestsReceived[0].RequestUri, Is.Not.Null);
            Assert.That(this._testHandler.RequestsReceived[0].RequestUri.ToString(),
                Does.Contain("maven/mavencentral/org.apache.commons/commons-lang3/3.12.0"));
        }

        [Test]
        public async Task GetClearlyDefinedLicensesAsync_WhenApiReturnsError_ReturnsNull()
        {
            // Arrange
            var packageUrl = new PackageURL("npm", null, "nonexistent", "1.0.0", null, null);
            var provider = Provider.Npmjs;

            this._testHandler.ResponseToReturn = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("")
            };

            // Act
            var result = await this._client.GetClearlyDefinedLicensesAsync(packageUrl, provider);

            // Assert
            Assert.That(result, Is.Null);

            this._logger.Received(1).Log(
                Arg.Is(LogLevel.Error),
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("API call unsuccessful")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        [Test]
        public async Task GetClearlyDefinedLicensesAsync_WhenApiThrowsException_ReturnsNull()
        {
            // Arrange
            var packageUrl = new PackageURL("npm", null, "lodash", "4.17.21", null, null);
            var provider = Provider.Npmjs;

            this._testHandler.ExceptionToThrow = new HttpRequestException("Netzwerkfehler");

            // Act
            var result = await this._client.GetClearlyDefinedLicensesAsync(packageUrl, provider);

            // Assert
            Assert.That(result, Is.Null);

            this._logger.Received(1).Log(
                Arg.Is(LogLevel.Error),
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Is<Exception>(ex => ex is HttpRequestException),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        [Test]
        public async Task GetClearlyDefinedLicensesAsync_WhenRateLimited_RetriesAndSucceeds()
        {
            // Arrange
            var packageUrl = new PackageURL("npm", null, "lodash", "4.17.21", null, null);
            var provider = Provider.Npmjs;

            var rateLimitResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.TooManyRequests,
                Content = new StringContent("")
            };
            rateLimitResponse.Headers.Add("x-ratelimit-remaining", "0");
            rateLimitResponse.Headers.Add("x-ratelimit-limit", "250");

            var successResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new ClearlyDefinedResponse
                {
                    Licensed = new ClearlyDefinedResponse.LicensedData
                    {
                        Facets = new ClearlyDefinedResponse.Facets
                        {
                            Core = new ClearlyDefinedResponse.Core
                            {
                                Discovered = new ClearlyDefinedResponse.Discovered
                                {
                                    Expressions = ["MIT"]
                                }
                            }
                        }
                    }
                }))
            };

            this._testHandler.ResponseSequence.Add(rateLimitResponse);
            this._testHandler.ResponseSequence.Add(successResponse);

            // Act
            var result = await this._client.GetClearlyDefinedLicensesAsync(packageUrl, provider);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0], Is.EqualTo("MIT"));

            Assert.That(this._testHandler.RequestsReceived.Count, Is.EqualTo(2));

            this._logger.Received(1).Log(
                Arg.Is(LogLevel.Warning),
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("Rate limit reached")),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }
    }

    // Hilfsklasse für HTTP-Tests
    public class TestHttpMessageHandler : HttpMessageHandler
    {
        private int _sequenceIndex;
        public List<HttpRequestMessage> RequestsReceived { get; } = new();
        public HttpResponseMessage ResponseToReturn { get; set; } = new(HttpStatusCode.OK);
        public List<HttpResponseMessage> ResponseSequence { get; } = new();
        public Exception? ExceptionToThrow { get; set; }

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