using System;
using Microsoft.CodeAnalysis;

namespace CleanCode.Analyzers.Test.TestHelper
{
    public struct DiagnosticResult
    {
        private DiagnosticResultLocation[] locations;

        public DiagnosticResultLocation[] Locations
        {
            get
            {
                if (locations == null)
                {
                    return Array.Empty<DiagnosticResultLocation>();
                }

                return locations;
            }

            set { locations = value; }
        }

        public DiagnosticSeverity Severity { get; set; }

        public string Id { get; set; }

        public string Message { get; set; }

        public string Path
        {
            get { return Locations.Length > 0 ? Locations[0].Path : ""; }
        }

        public int Line
        {
            get { return Locations.Length > 0 ? Locations[0].Line : -1; }
        }

        public int Column
        {
            get { return Locations.Length > 0 ? Locations[0].Column : -1; }
        }
    }
}