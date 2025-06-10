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
            this._fixture.SetupSuccessResponse(["MIT"]);

            // Act
            var result = await this._fixture.Client.GetClearlyDefinedLicensesAsync(packageUrl, provider);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0], Is.EqualTo("MIT"));

            this._fixture.VerifyRequestUri("npm/npmjs/-/lodash/4.17.21");
        }

        [Test]
        public async Task GetClearlyDefinedLicensesAsync_WithNamespace_ConstructsCorrectUrl()
        {
            // Arrange
            var packageUrl = new PackageURL("maven", "org.apache.commons", "commons-lang3", "3.12.0", null, null);
            var provider = Provider.MavenCentral;
            this._fixture.SetupSuccessResponse(["Apache-2.0"]);

            // Act
            var result = await this._fixture.Client.GetClearlyDefinedLicensesAsync(packageUrl, provider);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));

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
            var result = await this._fixture.Client.GetClearlyDefinedLicensesAsync(packageUrl, provider);

            // Assert
            Assert.That(result, Is.Null);

            this._fixture.VerifyLoggerReceivedError("API call unsuccessful");
        }

        [Test]
        public async Task GetClearlyDefinedLicensesAsync_WhenApiThrowsException_ReturnsNull()
        {
            // Arrange
            var packageUrl = new PackageURL("npm", null, "lodash", "4.17.21", null, null);
            var provider = Provider.Npmjs;
            this._fixture.SetupExceptionResponse(new HttpRequestException("Netzwerkfehler"));

            // Act
            var result = await this._fixture.Client.GetClearlyDefinedLicensesAsync(packageUrl, provider);

            // Assert
            Assert.That(result, Is.Null);

            this._fixture.VerifyLoggerReceivedExceptionOfType<HttpRequestException>();
        }

        [Test]
        public async Task GetClearlyDefinedLicensesAsync_WhenRateLimited_RetriesAndSucceeds()
        {
            // Arrange
            var packageUrl = new PackageURL("npm", null, "lodash", "4.17.21", null, null);
            var provider = Provider.Npmjs;
            this._fixture.SetupRateLimitThenSuccessResponse(["MIT"]);

            // Act
            var result = await this._fixture.Client.GetClearlyDefinedLicensesAsync(packageUrl, provider);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0], Is.EqualTo("MIT"));

            Assert.That(this._fixture.HttpHandler.RequestsReceived.Count, Is.EqualTo(2));

            this._fixture.VerifyLoggerReceivedWarning("Rate limit reached");
        }
    }

    /// <summary>
    ///     Fixture-Klasse für ClearlyDefinedClient-Tests
    /// </summary>
    public class ClearlyDefinedClientFixture : IDisposable
    {
        public ClearlyDefinedClientFixture()
        {
            this.Logger = Substitute.For<ILogger<ClearlyDefinedClient>>();
            this.HttpHandler = new TestHttpMessageHandler();
            this.HttpClient = new HttpClient(this.HttpHandler);
            this.Client = new ClearlyDefinedClient(this.HttpClient, this.Logger);
        }

        public ILogger<ClearlyDefinedClient> Logger { get; }
        public TestHttpMessageHandler HttpHandler { get; }
        public HttpClient HttpClient { get; }
        public ClearlyDefinedClient Client { get; }

        public void Dispose()
        {
            this.HttpClient.Dispose();
            this.HttpHandler.Dispose();
        }

        /// <summary>
        ///     Richtet eine erfolgreiche Antwort mit den angegebenen Lizenzausdrücken ein
        /// </summary>
        public void SetupSuccessResponse(List<string> licenseExpressions)
        {
            var responseContent = CreateResponseWithLicenses(licenseExpressions);
            var json = JsonSerializer.Serialize(responseContent);

            this.HttpHandler.ResponseToReturn = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            };
        }

        /// <summary>
        ///     Richtet eine Fehlerantwort mit dem angegebenen Statuscode ein
        /// </summary>
        public void SetupErrorResponse(HttpStatusCode statusCode)
        {
            this.HttpHandler.ResponseToReturn = new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent("")
            };
        }

        /// <summary>
        ///     Richtet eine Ausnahme als Antwort ein
        /// </summary>
        public void SetupExceptionResponse(Exception exception)
        {
            this.HttpHandler.ExceptionToThrow = exception;
        }

        /// <summary>
        ///     Richtet eine Ratengrenze und dann eine erfolgreiche Antwort ein
        /// </summary>
        public void SetupRateLimitThenSuccessResponse(List<string> licenseExpressions)
        {
            var rateLimitResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.TooManyRequests,
                Content = new StringContent("")
            };
            rateLimitResponse.Headers.Add("x-ratelimit-remaining", "0");
            rateLimitResponse.Headers.Add("x-ratelimit-limit", "250");

            var responseContent = CreateResponseWithLicenses(licenseExpressions);
            var successResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(responseContent))
            };

            this.HttpHandler.ResponseSequence.Add(rateLimitResponse);
            this.HttpHandler.ResponseSequence.Add(successResponse);
        }

        /// <summary>
        ///     Überprüft, ob die Anfrage-URI den angegebenen String enthält
        /// </summary>
        public void VerifyRequestUri(string contains)
        {
            Assert.That(this.HttpHandler.RequestsReceived.Count, Is.GreaterThan(0));
            Assert.That(this.HttpHandler.RequestsReceived[0].Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(this.HttpHandler.RequestsReceived[0].RequestUri, Is.Not.Null);
            Assert.That(this.HttpHandler.RequestsReceived[0].RequestUri.ToString(), Does.Contain(contains));
        }

        /// <summary>
        ///     Überprüft, ob der Logger eine Fehlermeldung mit dem angegebenen Text erhalten hat
        /// </summary>
        public void VerifyLoggerReceivedError(string messageContains)
        {
            this.Logger.Received(1).Log(
                Arg.Is(LogLevel.Error),
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains(messageContains)),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        /// <summary>
        ///     Überprüft, ob der Logger eine Warnmeldung mit dem angegebenen Text erhalten hat
        /// </summary>
        public void VerifyLoggerReceivedWarning(string messageContains)
        {
            this.Logger.Received(1).Log(
                Arg.Is(LogLevel.Warning),
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains(messageContains)),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        /// <summary>
        ///     Überprüft, ob der Logger eine Ausnahme vom angegebenen Typ erhalten hat
        /// </summary>
        public void VerifyLoggerReceivedExceptionOfType<T>() where T : Exception
        {
            this.Logger.Received(1).Log(
                Arg.Is(LogLevel.Error),
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Is<Exception>(ex => ex is T),
                Arg.Any<Func<object, Exception, string>>()!);
        }

        /// <summary>
        ///     Erstellt eine ClearlyDefinedResponse mit den angegebenen Lizenzausdrücken
        /// </summary>
        private static ClearlyDefinedResponse CreateResponseWithLicenses(List<string> expressions)
        {
            return new ClearlyDefinedResponse
            {
                Licensed = new ClearlyDefinedResponse.LicensedData
                {
                    Facets = new ClearlyDefinedResponse.Facets
                    {
                        Core = new ClearlyDefinedResponse.Core
                        {
                            Discovered = new ClearlyDefinedResponse.Discovered
                            {
                                Expressions = expressions
                            }
                        }
                    }
                }
            };
        }
    }

    // Hilfsklasse für HTTP-Tests
    public class TestHttpMessageHandler : HttpMessageHandler, IDisposable
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