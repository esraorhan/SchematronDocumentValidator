using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchematronDocumentValidator.Infrastructure.Saxon.Compilation
{
    public sealed class SchematronCompilationArtifacts
    {
        public required string IncludedSchematronPath { get; init; }

        public required string ExpandedSchematronPath { get; init; }

        public required string GeneratedValidatorXsltPath { get; init; }
    }
}
