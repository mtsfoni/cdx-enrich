using CdxEnrich.ClearlyDefined;
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

        [TestCase("MIT OR Apache-2.0", "MIT", "Apache-2.0",
            Description = "Multiple expressions")]
        [TestCase("MIT", "MIT",
            Description = "One expressions, no operator")]
        [TestCase("MIT OR Apache-2.0", "MIT OR Apache-2.0",
            Description = "One expressions, with OR operator")]
        [TestCase("MIT WITH Apache-2.0", "MIT WITH Apache-2.0",
            Description = "One expressions, with WITH operator")]
        [TestCase("MIT AND Apache-2.0", "MIT AND Apache-2.0",
            Description = "One expressions, with AND operator")]
        public void
            Resolve_WhenDeclaredContainsOTHER_AndTryGetLicenseFromExpressionsSucceeds_ReturnsLicenseChoiceWithExpression(
                string expected, params string[] expressions)
        {
            // Arrange
            var dataLicensed = this._fixture.CreateLicenseDeclaredWithOtherAndExpressions(
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

        [TestCase("LicenseRef-scancode-unknown-license-reference",
            Description = "One expression 'LicenseRef-scancode-unknown-license-reference' without operator")]
        [TestCase("MIT OR LicenseRef-scancode-unknown-license-reference",
            Description = "One expression  'LicenseRef-scancode-unknown-license-reference' with operator before it")]
        [TestCase("LicenseRef-scancode-unknown-license-reference OR MIT",
            Description = "One expression  'LicenseRef-scancode-unknown-license-reference' with operator after it")]
        [TestCase("MIT", "LicenseRef-scancode-unknown-license-reference",
            Description =
                "Multiple expressions 'LicenseRef-scancode-unknown-license-reference' expression with operator before it")]
        [TestCase("LicenseRef-scancode-unknown-license-reference", "MIT",
            Description =
                "Multiple expressions 'LicenseRef-scancode-unknown-license-reference' expression with operator after it")]
        [TestCase("OTHER",
            Description = "Single expression 'OTHER' without operator")]
        public void Resolve_WhenDeclaredContainsOTHER_AndTryGetLicenseFromMultipleExpressionsFails_ReturnsNull(
            params string[] expressions)
        {
            // Arrange
            var dataLicensed = this._fixture.CreateLicenseDeclaredWithOtherAndExpressions(
                expressions.ToList());

            // Act
            var result = this._fixture.Resolver.Resolve(this._fixture.PackageUrl, dataLicensed);

            // Assert
            Assert.That(result, Is.Null);
        }
        
        [TestCase("MIT OR Apache-2.0", "MIT", "Apache-2.0",
            Description = "Multiple expressions")]
        [TestCase("MIT", "MIT",
            Description = "One expressions, no operator")]
        [TestCase("MIT OR Apache-2.0", "MIT OR Apache-2.0",
            Description = "One expressions, with OR operator")]
        [TestCase("MIT WITH Apache-2.0", "MIT WITH Apache-2.0",
            Description = "One expressions, with WITH operator")]
        [TestCase("MIT AND Apache-2.0", "MIT AND Apache-2.0",
            Description = "One expressions, with AND operator")]
        public void
            Resolve_WhenDeclaredContainsNONE_AndTryGetLicenseFromExpressionsSucceeds_ReturnsLicenseChoiceWithExpression(
                string expected, params string[] expressions)
        {   
            // Arrange
            var dataLicensed = this._fixture.CreateLicenseDeclaredWithNoneAndExpressions(
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

        [TestCase("LicenseRef-scancode-unknown-license-reference",
            Description = "One expression 'LicenseRef-scancode-unknown-license-reference' without operator")]
        [TestCase("MIT OR LicenseRef-scancode-unknown-license-reference",
            Description = "One expression  'LicenseRef-scancode-unknown-license-reference' with operator before it")]
        [TestCase("LicenseRef-scancode-unknown-license-reference OR MIT",
            Description = "One expression  'LicenseRef-scancode-unknown-license-reference' with operator after it")]
        [TestCase("MIT", "LicenseRef-scancode-unknown-license-reference",
            Description =
                "Multiple expressions 'LicenseRef-scancode-unknown-license-reference' expression with operator before it")]
        [TestCase("LicenseRef-scancode-unknown-license-reference", "MIT",
            Description =
                "Multiple expressions 'LicenseRef-scancode-unknown-license-reference' expression with operator after it")]
        [TestCase("NONE",
            Description = "Single expression 'NONE' without operator")]
        public void Resolve_WhenDeclaredContainsNONE_AndTryGetLicenseFromMultipleExpressionsFails_ReturnsNull(
            params string[] expressions)
        {
            // Arrange
            var dataLicensed = this._fixture.CreateLicenseDeclaredWithNoneAndExpressions(
                expressions.ToList());

            // Act
            var result = this._fixture.Resolver.Resolve(this._fixture.PackageUrl, dataLicensed);

            // Assert
            Assert.That(result, Is.Null);
        }
        
        [TestCase("MIT OR Apache-2.0", "MIT", "Apache-2.0",
            Description = "Multiple expressions")]
        [TestCase("MIT", "MIT",
            Description = "One expressions, no operator")]
        [TestCase("MIT OR Apache-2.0", "MIT OR Apache-2.0",
            Description = "One expressions, with OR operator")]
        [TestCase("MIT WITH Apache-2.0", "MIT WITH Apache-2.0",
            Description = "One expressions, with WITH operator")]
        [TestCase("MIT AND Apache-2.0", "MIT AND Apache-2.0",
            Description = "One expressions, with AND operator")]
        public void
            Resolve_WhenDeclaredContainsNOASSERTION_AndTryGetLicenseFromExpressionsSucceeds_ReturnsLicenseChoiceWithExpression(
                string expected, params string[] expressions)
        {   
            // Arrange
            var dataLicensed = this._fixture.CreateLicenseDeclaredWithNoAssertionAndExpressions(
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

        [TestCase("LicenseRef-scancode-unknown-license-reference",
            Description = "One expression 'LicenseRef-scancode-unknown-license-reference' without operator")]
        [TestCase("MIT OR LicenseRef-scancode-unknown-license-reference",
            Description = "One expression  'LicenseRef-scancode-unknown-license-reference' with operator before it")]
        [TestCase("LicenseRef-scancode-unknown-license-reference OR MIT",
            Description = "One expression  'LicenseRef-scancode-unknown-license-reference' with operator after it")]
        [TestCase("MIT", "LicenseRef-scancode-unknown-license-reference",
            Description =
                "Multiple expressions 'LicenseRef-scancode-unknown-license-reference' expression with operator before it")]
        [TestCase("LicenseRef-scancode-unknown-license-reference", "MIT",
            Description =
                "Multiple expressions 'LicenseRef-scancode-unknown-license-reference' expression with operator after it")]
        [TestCase("NOASSERTION",
            Description = "Single expression 'NOASSERTION' without operator")]
        public void Resolve_WhenDeclaredContainsNOASSERTION_AndTryGetLicenseFromMultipleExpressionsFails_ReturnsNull(
            params string[] expressions)
        {
            // Arrange
            var dataLicensed = this._fixture.CreateLicenseDeclaredWithNoAssertionAndExpressions(
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
            public LicenseResolver Resolver { get; } = new();
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

            public ClearlyDefinedResponse.LicensedData CreateLicenseDeclaredWithOtherAndExpressions(
                List<string> expressions)
            {
                return this.CreateLicenseDeclaredAndExpressions("OTHER", expressions);
            }
            
            public ClearlyDefinedResponse.LicensedData CreateLicenseDeclaredWithNoneAndExpressions(
                List<string> expressions)
            {
                return this.CreateLicenseDeclaredAndExpressions("NONE", expressions);
            }
            
            public ClearlyDefinedResponse.LicensedData CreateLicenseDeclaredWithNoAssertionAndExpressions(
                List<string> expressions)
            {
                return this.CreateLicenseDeclaredAndExpressions("NOASSERTION", expressions);
            }
        }
    }
}