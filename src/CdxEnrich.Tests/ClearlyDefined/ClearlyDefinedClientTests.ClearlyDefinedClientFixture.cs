using System.Net;
using System.Text.Json;
using CdxEnrich.ClearlyDefined;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CdxEnrich.Tests.ClearlyDefined
{
    public partial class ClearlyDefinedClientTests
    {
        private class ClearlyDefinedClientFixture : IDisposable
        {
            public ClearlyDefinedClientFixture()
            {
                this.Logger = Substitute.For<ILogger<ClearlyDefinedClient>>();
                this.HttpHandler = new TestHttpMessageHandler();
                this.HttpClient = new HttpClient(this.HttpHandler)
                {
                    BaseAddress = ClearlyDefinedClient.ClearlyDefinedApiBaseAddress
                };
                var httpClientFactoryMock = Substitute.For<IHttpClientFactory>();
                httpClientFactoryMock.CreateClient(Arg.Any<string>()).Returns(this.HttpClient);
                this.Client = new ClearlyDefinedClient(this.Logger, httpClientFactoryMock);
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
            ///     Sets up a successful response with the specified license expressions
            /// </summary>
            public void SetupSuccessResponse(string declared, List<string> licenseExpressions)
            {
                var responseContent = CreateResponseWithLicenses(declared, licenseExpressions);
                var json = JsonSerializer.Serialize(responseContent);

                this.HttpHandler.ResponseToReturn = new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(json)
                };
            }

            /// <summary>
            ///     Sets up an error response with the specified status code
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
            ///     Sets up an exception as a response
            /// </summary>
            public void SetupExceptionResponse(Exception exception)
            {
                this.HttpHandler.ExceptionToThrow = exception;
            }

            /// <summary>
            ///     Sets up a rate limit response followed by a successful response
            /// </summary>
            public void SetupRateLimitThenSuccessResponse(string declared, List<string> licenseExpressions)
            {
                var rateLimitResponse = new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.TooManyRequests,
                    Content = new StringContent("")
                };
                rateLimitResponse.Headers.Add("x-ratelimit-remaining", "0");
                rateLimitResponse.Headers.Add("x-ratelimit-limit", "250");

                var responseContent = CreateResponseWithLicenses(declared, licenseExpressions);
                var successResponse = new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(responseContent))
                };

                this.HttpHandler.ResponseSequence.Add(rateLimitResponse);
                this.HttpHandler.ResponseSequence.Add(successResponse);
            }

            /// <summary>
            ///     Verifies that the request URI contains the specified string
            /// </summary>
            public void VerifyRequestUri(string contains)
            {
                Assert.That(this.HttpHandler.RequestsReceived.Count, Is.GreaterThan(0));
                Assert.That(this.HttpHandler.RequestsReceived[0].Method, Is.EqualTo(HttpMethod.Get));
                Assert.That(this.HttpHandler.RequestsReceived[0].RequestUri, Is.Not.Null);
                Assert.That(this.HttpHandler.RequestsReceived[0].RequestUri.ToString(), Does.Contain(contains));
            }

            /// <summary>
            ///     Verifies that the logger received an error message containing the specified text
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
            ///     Verifies that the logger received a warning message containing the specified text
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
            ///     Verifies that the logger received an exception of the specified type
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
            ///     Creates a ClearlyDefinedResponse with the specified license expressions
            /// </summary>
            private static ClearlyDefinedResponse CreateResponseWithLicenses(string declared, List<string> expressions)
            {
                return new ClearlyDefinedResponse
                {
                    Licensed = new ClearlyDefinedResponse.LicensedData
                    {
                        Declared = declared,
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
    }
}