using CdxEnrich.ClearlyDefined;
using CdxEnrich.ClearlyDefined.Rules;
using CdxEnrich.Logging;
using Microsoft.Extensions.Logging;
using PackageUrl;

namespace CdxEnrich.Tests.ClearlyDefined
{
    [TestFixture]
    public class LicenseResolverTests
    {
        private LicenseResolverFixture _fixture;

        [SetUp]
        public void Setup()
        {
            this._fixture = new LicenseResolverFixture();
        }
        
        public static IEnumerable<object[]> SuccessLicensePlaceholderWithExpression()
        {
            foreach (var licensePlaceholder in LicensePlaceholder.All)
            {
                yield return ["MIT OR Apache-2.0", licensePlaceholder, new [] {"MIT", "Apache-2.0"}]; //Multiple expressions
                yield return ["MIT OR Apache-2.0", licensePlaceholder,  new [] {"MIT OR Apache-2.0"}]; //One expressions, with OR operator
                yield return ["MIT WITH Apache-2.0", licensePlaceholder,  new [] {"MIT WITH Apache-2.0"}]; //One expressions, with WITH operator
                yield return ["MIT AND Apache-2.0", licensePlaceholder,  new [] {"MIT AND Apache-2.0"}]; //One expressions, with AND operator
                yield return ["MIT", licensePlaceholder, new []{"MIT"}]; //One expressions, no operator
            }
        }
        
        public static IEnumerable<object[]> FailingLicensePlaceholderWithExpression()
        {
            foreach (var licensePlaceholder in LicensePlaceholder.All)
            {
                yield return [licensePlaceholder, new [] {"LicenseRef-scancode-unknown-license-reference"}]; //One expression 'LicenseRef-scancode-unknown-license-reference' without operator
                yield return [licensePlaceholder,  new [] {"MIT OR LicenseRef-scancode-unknown-license-reference"}]; //One expression 'LicenseRef-scancode-unknown-license-reference' with operator before it
                yield return [licensePlaceholder,  new [] {"LicenseRef-scancode-unknown-license-reference OR MIT"}]; //One expression 'LicenseRef-scancode-unknown-license-reference' with operator after it
                yield return [licensePlaceholder,  new [] {"MIT", "LicenseRef-scancode-unknown-license-reference"}]; // Multiple expressions 'LicenseRef-scancode-unknown-license-reference' expression with operator before it
                yield return [licensePlaceholder,  new [] {"LicenseRef-scancode-unknown-license-reference", "MIT"}]; //Multiple expressions 'LicenseRef-scancode-unknown-license-reference' expression with operator after it
                yield return [licensePlaceholder, Array.Empty<string>()]; //Without any expressions
                foreach (var licensePlaceholderFromExpression in LicensePlaceholder.All)
                {
                    yield return [licensePlaceholder, new[] { licensePlaceholderFromExpression.LicenseIdentifier }]; //One expression that is a license placeholder
                }
            }
        }
        
        [Test]
        [TestCaseSource(nameof(SuccessLicensePlaceholderWithExpression))]
        public void Resolve_WhenDeclaredContainsLicensePlaceholder_AndTryGetLicenseFromExpressionsSucceeds_ReturnsLicenseChoiceWithExpression(
            string expected, LicensePlaceholder licensePlaceholder, string[] expressions)
        {
            // Arrange
            var dataLicensed = this._fixture.CreateLicenseDeclaredForLicensePlaceholderAndExpressions(
                licensePlaceholder,
                expressions.ToList());

            // Act
            var result = this._fixture.Resolver.Resolve(this._fixture.PackageUrl, dataLicensed);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Expression, Is.EqualTo(expected));
                Assert.That(result.License, Is.Null);
            });
        }

        [Test]
        [TestCaseSource(nameof(FailingLicensePlaceholderWithExpression))]
        public void Resolve_WhenDeclaredContainsLicensePlaceholder_AndTryGetLicenseFromMultipleExpressionsFails_ReturnsNull(
            LicensePlaceholder licensePlaceholder, string[] expressions)
        {
            // Arrange
            var dataLicensed = this._fixture.CreateLicenseDeclaredForLicensePlaceholderAndExpressions(
                licensePlaceholder,
                expressions.ToList());

            // Act
            var result = this._fixture.Resolver.Resolve(this._fixture.PackageUrl, dataLicensed);

            // Assert
            Assert.That(result, Is.Null);
        }
        
        [Test]
        public void Resolve_WhenDeclaredIsNotExpressionAndIsNotLicenseRef_ReturnsLicenseChoiceWithLicenseId()
        {
            // Arrange
            var dataLicensed = this._fixture.CreateLicenseDeclaredWith("MIT");

            // Act
            var result = this._fixture.Resolver.Resolve(this._fixture.PackageUrl, dataLicensed);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Expression, Is.Null);
            Assert.That(result.License, Is.Not.Null);
            Assert.That(result.License.Id, Is.EqualTo("MIT"));
        }

        [Test]
        public void Resolve_WhenDeclaredIsExpression_ReturnsLicenseChoiceWithExpression()
        {
            // Arrange
            var dataLicensed = this._fixture.CreateLicenseDeclaredWith("MIT OR Apache-2.0");

            // Act
            var result = this._fixture.Resolver.Resolve(this._fixture.PackageUrl, dataLicensed);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Expression, Is.EqualTo("MIT OR Apache-2.0"));
                Assert.That(result.License, Is.Null);
            });
        }

        [Test]
        public void Resolve_WhenDeclaredIsLicenseRef_ReturnsLicenseChoiceWithExpression()
        {
            // Arrange
            var dataLicensed = this._fixture.CreateLicenseDeclaredWith("LicenseRef-scancode-ms-net-library-2018-11");

            // Act
            var result = this._fixture.Resolver.Resolve(this._fixture.PackageUrl, dataLicensed);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Expression, Is.EqualTo("LicenseRef-scancode-ms-net-library-2018-11"));
                Assert.That(result.License, Is.Null);
            });
        }

        [TestCase("MIT AND Apache-2.0", "MIT AND Apache-2.0",
            Description = "AND operator in the declared license")]
        [TestCase("MIT OR Apache-2.0", "MIT OR Apache-2.0",
            Description = "OR operator in the declared license")]
        [TestCase("MIT WITH Apache-2.0", "MIT WITH Apache-2.0",
            Description = "WITH operator in the declared license")]
        public void Resolve_WhenDeclaredContainsOperator_ReturnsLicenseChoiceWithExpression(string declared,
            string expected)
        {
            // Arrange
            var dataLicensed = this._fixture.CreateLicenseDeclaredWith(declared);

            // Act
            var result = this._fixture.Resolver.Resolve(this._fixture.PackageUrl, dataLicensed);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Expression, Is.EqualTo(expected));
                Assert.That(result.License, Is.Null);
            });
        }

        [Test]
        public void Resolve_WhenDeclaredContainsWITH_ReturnsLicenseChoiceWithExpression()
        {
            // Arrange
            var dataLicensed = this._fixture.CreateLicenseDeclaredWith("GPL-2.0-or-later WITH Classpath-exception-2.0");

            // Act
            var result = this._fixture.Resolver.Resolve(this._fixture.PackageUrl, dataLicensed);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Expression, Is.EqualTo("GPL-2.0-or-later WITH Classpath-exception-2.0"));
                Assert.That(result.License, Is.Null);
            });
        }

        [Test]
        public void Resolve_WhenDeclaredIsDefinedLicense_AndExpressionsExist_UsesDeclaredAndIgnoresExpressions()
        {
            // Arrange
            var dataLicensed = this._fixture.CreateLicenseDeclaredAndExpressions(
                "MIT",
                ["Apache-2.0", "GPL-3.0"]);

            // Act
            var result = this._fixture.Resolver.Resolve(this._fixture.PackageUrl, dataLicensed);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Expression, Is.Null);
                Assert.That(result.License, Is.Not.Null);
            });
            Assert.That(result.License.Id, Is.EqualTo("MIT"));
        }

        private class LicenseResolverFixture
        {
            public LicenseResolver Resolver { get; } = new (new ConsoleLogger<LicenseResolver>(), new ResolveLicenseRuleFactory(new LoggerFactory()));
            public PackageURL PackageUrl { get; } = new("nuget", null, "Test.Package", "1.0.0", null, null);

            public ClearlyDefinedResponse.LicensedData CreateLicenseDeclaredWith(string declared)
            {
                return new ClearlyDefinedResponse.LicensedData
                {
                    Declared = declared,
                    Facets = new ClearlyDefinedResponse.Facets
                    {
                        Core = new ClearlyDefinedResponse.Core
                        {
                            Discovered = new ClearlyDefinedResponse.Discovered
                            {
                                Expressions = []
                            }
                        }
                    }
                };
            }

            public ClearlyDefinedResponse.LicensedData CreateLicenseDeclaredAndExpressions(string declared,
                List<string> expressions)
            {
                return new ClearlyDefinedResponse.LicensedData
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
                };
            }

            public ClearlyDefinedResponse.LicensedData CreateLicenseDeclaredForLicensePlaceholderAndExpressions(
                LicensePlaceholder licensePlaceholder, List<string> expressions)
            {
                return this.CreateLicenseDeclaredAndExpressions(licensePlaceholder.LicenseIdentifier, expressions);
            }
        }
    }
}