using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CleanCode.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TestAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "TestAnalyzer";

        public static readonly string Title = "TestAnalyzer Title";
        public static readonly string MessageFormat = "TestAnalyzer Message";
        private static readonly string Description = "TestAnalyzer Description";
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId, Title, MessageFormat, Category,
            DiagnosticSeverity.Warning, isEnabledByDefault:
            true, description: Description);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var diagnostic = Diagnostic.Create(Rule, context.Node.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}