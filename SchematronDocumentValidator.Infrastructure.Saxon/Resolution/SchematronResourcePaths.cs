using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchematronDocumentValidator.Infrastructure.Saxon.Resolution
{
    public sealed class SchematronResourcePaths //Çalışma anında gerçekten kullanılacak fiziksel yolları ifade eder.
    { 
        public required string MainSchematronPath { get; init; }

        public required string IncludeXsltPath { get; init; }

        public required string AbstractExpandXsltPath { get; init; }

        public required string MessageXsltPath { get; init; }

        public required string GeneratedDirectoryPath { get; init; }
    }
}
