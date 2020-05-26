using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace CleanCode.Analyzers.Test.TestHelper
{
    internal static class TestDocumentsFactory
    {
        private const string TestProjectName = "TestProject";

        private static readonly MetadataReference CorlibReference =
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location);

        private static readonly MetadataReference SystemCoreReference =
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);

        private static readonly MetadataReference CSharpSymbolsReference =
            MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location);

        private static readonly MetadataReference CodeAnalysisReference =
            MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location);
        
        public static Document[] CreateDocumentsFrom(string[] sources)
        {
            var project = CreateProject(sources);
            
            return project.Documents.ToArray();
        }

        private static Project CreateProject(string[] sources)
        {
            var projectId = ProjectId.CreateNewId(TestProjectName);
            var projectInfo = ProjectInfo.Create(projectId, default, TestProjectName,
                TestProjectName, LanguageNames.CSharp);

            var solution = new AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectInfo)
                .AddMetadataReference(projectId, CorlibReference)
                .AddMetadataReference(projectId, SystemCoreReference)
                .AddMetadataReference(projectId, CSharpSymbolsReference)
                .AddMetadataReference(projectId, CodeAnalysisReference);

            int count = 0;
            foreach (string source in sources)
            {
                var newFileName = $"Test{count}.cs";
                var documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
                solution = solution.AddDocument(documentId, newFileName, SourceText.From(source));
                count++;
            }

            return solution.GetProject(projectId);
        }
    }
}