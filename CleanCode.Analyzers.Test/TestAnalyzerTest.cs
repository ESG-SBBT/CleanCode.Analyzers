using CleanCode.Analyzers.Test.TestHelper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CleanCode.Analyzers.Test
{
    [TestClass]
    public class TestAnalyzerTest : DiagnosticVerifier
    {
        [TestMethod]
        public void Test()
        {
            var test = @"
class TypeName
{
    public void Test()
    {
        var a = ""test"";
        System.String.Equals(a, ""v"");
    }
}";
            var expected = new DiagnosticResult
            {
                Id = TestAnalyzer.DiagnosticId,
                Message = "TestAnalyzer Message",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        // Test0.cs is the name of the file created by VerifyCSharpDiagnostic
                        new DiagnosticResultLocation("Test0.cs", line: 7, column: 9)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        protected override DiagnosticAnalyzer CreateAnalyzer()
        {
            return new TestAnalyzer();
        }
    }
}