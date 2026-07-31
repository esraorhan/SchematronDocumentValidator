using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchematronDocumentValidator.Infrastructure.Saxon.Configuration
{
    public sealed class SchematronResourceOptions
    {
        public string ResourcesDirectory { get; init; } = "Resources";

        public string CompilerXsltDirectory { get; init; } =
            Path.Combine("SchematronCompiler","CompilerXslt");

        public string GibEFaturaPackageDirectory { get; init; } =
            Path.Combine("ValidationPackages", "GibEFatura");

        public string GeneratedDirectory { get; init; } = "Generated";

        public string MainSchematronFileName { get; init; } = "UBL-TR_Main_Schematron.xml";

        public string IncludeXsltFileName { get; init; } = "iso_dsdl_include.xsl";

        public string AbstractExpandXsltFileName { get; init; } = "iso_abstract_expand.xsl";

        public string MessageXsltFileName { get; init; } = "iso_plain_text_for_xslt2.xsl";
    }
}
