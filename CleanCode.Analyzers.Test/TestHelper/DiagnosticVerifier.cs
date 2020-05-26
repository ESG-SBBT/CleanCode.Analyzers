using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CleanCode.Analyzers.Test.TestHelper
{
    public abstract class DiagnosticVerifier
    {
        private string[] sources;
        private DiagnosticAnalyzer analyzer;

        private Diagnostic[] actualResults;
        private DiagnosticResult[] expectedResults;
        
        protected abstract DiagnosticAnalyzer CreateAnalyzer();

        protected void VerifyCSharpDiagnostic(string source, params DiagnosticResult[] expected)
        {
            VerifyCSharpDiagnostic(new[] {source}, expected);
        }

        private void VerifyCSharpDiagnostic(string[] sources, params DiagnosticResult[] expected)
        {
            this.sources = sources;
            this.analyzer = CreateAnalyzer();
            this.expectedResults = expected;
            VerifyDiagnostics();
        }

        private void VerifyDiagnostics()
        {
            this.actualResults = GetSortedDiagnostics();
            VerifyDiagnosticResults();
        }

        private void VerifyDiagnosticResults()
        {
            VerifyDiagnosticCount();

            for (var i = 0; i < this.expectedResults.Length; i++)
                VerifyDiagnostic(i);
        }

        private void VerifyDiagnosticCount()
        {
            var expectedCount = this.expectedResults.Length;
            var actualCount = this.actualResults.Length;
            
            if (expectedCount != actualCount)
            {
                var diagnosticsOutput = this.actualResults.Any()
                    ? FormatDiagnostics(this.actualResults)
                    : "    NONE.";

                Assert.Fail($"Mismatch between number of diagnostics returned, expected \"{expectedCount}\" actual \"{actualCount}\"\r\n\r\nDiagnostics:\r\n{diagnosticsOutput}\r\n");
            }
        }

        private void VerifyDiagnostic(int i)
        {
            var actual = this.actualResults[i];
            var expected = this.expectedResults[i];

            if (expected.Line == -1 && expected.Column == -1)
            {
                if (actual.Location != Location.None)
                    Assert.Fail($"Expected:\nA project diagnostic with No location\nActual:\n{FormatDiagnostics(actual)}");
            }
            else
            {
                VerifyDiagnosticLocation(actual, actual.Location, expected.Locations.First());
                Location[] additionalLocations = actual.AdditionalLocations.ToArray();

                if (additionalLocations.Length != expected.Locations.Length - 1)
                    Assert.Fail($"Expected {expected.Locations.Length - 1} additional locations but got {additionalLocations.Length} for Diagnostic:\r\n    {FormatDiagnostics(actual)}\r\n");

                for (int j = 0; j < additionalLocations.Length; ++j)
                    VerifyDiagnosticLocation(actual, additionalLocations[j], expected.Locations[j + 1]);
            }

            if (actual.Id != expected.Id)
                Assert.Fail($"Expected diagnostic id to be \"{expected.Id}\" was \"{actual.Id}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(actual)}\r\n");

            if (actual.Severity != expected.Severity)
                Assert.Fail($"Expected diagnostic severity to be \"{expected.Severity}\" was \"{actual.Severity}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(actual)}\r\n");

            if (actual.GetMessage() != expected.Message)
                Assert.Fail($"Expected diagnostic message to be \"{expected.Message}\" was \"{actual.GetMessage()}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(actual)}\r\n");
        }

        private void VerifyDiagnosticLocation(Diagnostic diagnostic, Location actual, DiagnosticResultLocation expected)
        {
            FileLinePositionSpan actualSpan = actual.GetLineSpan();

            Assert.IsTrue(
                actualSpan.Path == expected.Path || (actualSpan.Path != null && actualSpan.Path.Contains("Test0.") &&
                                                     expected.Path.Contains("Test.")),
                $"Expected diagnostic to be in file \"{expected.Path}\" was actually in file \"{actualSpan.Path}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(diagnostic)}\r\n");

            LinePosition actualLinePosition = actualSpan.StartLinePosition;

            // Only check line position if there is an actual line in the real diagnostic
            if (expected.Line > 0)
                if (actualLinePosition.Line + 1 != expected.Line)
                    Assert.Fail($"Expected diagnostic to be on line \"{expected.Line}\" was actually on line \"{actualLinePosition.Line + 1}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(diagnostic)}\r\n");

            // Only check column position if there is an actual column position in the real diagnostic
            if (expected.Column > 0)
                if (actualLinePosition.Character + 1 != expected.Column)
                    Assert.Fail($"Expected diagnostic to start at column \"{expected.Column}\" was actually at column \"{actualLinePosition.Character + 1}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(diagnostic)}\r\n");
        }

        private string FormatDiagnostics(params Diagnostic[] diagnostics)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < diagnostics.Length; ++i)
            {
                builder.AppendLine("// " + diagnostics[i]);

                Type analyzerType = this.analyzer.GetType();
                ImmutableArray<DiagnosticDescriptor> rules = this.analyzer.SupportedDiagnostics;

                foreach (DiagnosticDescriptor rule in rules)
                {
                    if (rule != null && rule.Id == diagnostics[i].Id)
                    {
                        Location location = diagnostics[i].Location;
                        if (location == Location.None)
                        {
                            builder.AppendFormat("GetGlobalResult({0}.{1})", analyzerType.Name, rule.Id);
                        }
                        else
                        {
                            Assert.IsTrue(location.IsInSource,
                                $"Test base does not currently handle diagnostics in metadata locations. Diagnostic in metadata: {diagnostics[i]}\r\n");

                            string resultMethodName = diagnostics[i].Location.SourceTree.FilePath
                                .EndsWith(".cs", StringComparison.Ordinal)
                                ? "GetCSharpResultAt"
                                : "GetBasicResultAt";
                            LinePosition linePosition =
                                diagnostics[i].Location.GetLineSpan().StartLinePosition;

                            builder.AppendFormat("{0}({1}, {2}, {3}.{4})",
                                resultMethodName,
                                linePosition.Line + 1,
                                linePosition.Character + 1,
                                analyzerType.Name,
                                rule.Id);
                        }

                        if (i != diagnostics.Length - 1)
                        {
                            builder.Append(',');
                        }

                        builder.AppendLine();
                        break;
                    }
                }
            }

            return builder.ToString();
        }

        private Diagnostic[] GetSortedDiagnostics()
        {
            return GetSortedDiagnosticsFromDocuments(TestDocumentsFactory.CreateDocumentsFrom(this.sources));
        }

        private Diagnostic[] GetSortedDiagnosticsFromDocuments(Document[] documents)
        {
            HashSet<Project> projects = new HashSet<Project>();
            foreach (Document document in documents)
            {
                projects.Add(document.Project);
            }

            List<Diagnostic> diagnostics = new List<Diagnostic>();
            foreach (Project project in projects)
            {
                CompilationWithAnalyzers compilationWithAnalyzers =
                    project.GetCompilationAsync().Result.WithAnalyzers(ImmutableArray.Create(this.analyzer));
                ImmutableArray<Diagnostic> diags = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
                foreach (Diagnostic diag in diags)
                {
                    if (diag.Location == Location.None || diag.Location.IsInMetadata)
                    {
                        diagnostics.Add(diag);
                    }
                    else
                    {
                        for (int i = 0; i < documents.Length; i++)
                        {
                            Document document = documents[i];
                            SyntaxTree tree = document.GetSyntaxTreeAsync().Result;
                            if (tree == diag.Location.SourceTree)
                            {
                                diagnostics.Add(diag);
                            }
                        }
                    }
                }
            }

            Diagnostic[] results = SortDiagnostics(diagnostics);
            diagnostics.Clear();
            return results;
        }

        private static Diagnostic[] SortDiagnostics(IEnumerable<Diagnostic> diagnostics)
        {
            return diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();
        }
    }
}