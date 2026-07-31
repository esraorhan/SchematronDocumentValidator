using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SchematronDocumentValidator.Infrastructure.Saxon.Compilation
{
    public sealed class GeneratedValidatorPostProcessor
    {
        private static readonly XNamespace XslNamespace =
            "http://www.w3.org/1999/XSL/Transform";

        private const string ElementTraversal =
            "*|comment()|processing-instruction()";

        private const string AttributeAndElementTraversal =
            "@*|*|comment()|processing-instruction()";

        public void EnsureAttributeTraversal(string generatedXsltPath)
        {
            if (string.IsNullOrWhiteSpace(generatedXsltPath))
            {
                throw new ArgumentException(
                    "Üretilen XSLT yolu boş olamaz.",
                    nameof(generatedXsltPath));
            }

            if (!File.Exists(generatedXsltPath))
            {
                throw new FileNotFoundException(
                    "Üretilen validator XSLT bulunamadı.",
                    generatedXsltPath);
            }

            XDocument document = XDocument.Load(
                generatedXsltPath,
                LoadOptions.PreserveWhitespace);

            int changedCount = 0;

            IEnumerable<XElement> applyTemplatesElements =
                document.Descendants(
                    XslNamespace + "apply-templates");

            foreach (XElement applyTemplates in applyTemplatesElements)
            {
                XAttribute? selectAttribute =
                    applyTemplates.Attribute("select");

                if (selectAttribute?.Value != ElementTraversal)
                {
                    continue;
                }

                selectAttribute.Value =
                    AttributeAndElementTraversal;

                changedCount++;
            }

            if (changedCount == 0)
            {
                throw new InvalidOperationException(
                    "Üretilen XSLT içinde düzeltilecek dolaşım ifadesi bulunamadı.");
            }

            document.Save(
                generatedXsltPath,
                SaveOptions.DisableFormatting);
        }
    }
}
