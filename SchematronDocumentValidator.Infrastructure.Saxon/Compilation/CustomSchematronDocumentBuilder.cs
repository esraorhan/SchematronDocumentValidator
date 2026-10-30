using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Saxon.Hej.type;
using SchematronDocumentValidator.Core.Models;
namespace SchematronDocumentValidator.Infrastructure.Saxon.Compilation
{

    public sealed class CustomSchematronDocumentBuilder
    {
        private static readonly XNamespace Sch =
            "http://purl.oclc.org/dsdl/schematron";

        public XDocument Build(
            IReadOnlyCollection<CustomSchematronRule> rules)
        {
            ArgumentNullException.ThrowIfNull(rules);

            if (rules.Count == 0)
            {
                throw new ArgumentException(
                    "Schematron oluşturmak için en az bir kural gereklidir.",
                    nameof(rules));
            }

            // 1. Schematron belgesinin kökünü oluştur.
            var schema = new XElement(
                Sch + "schema",
                new XAttribute(XNamespace.Xmlns + "sch", Sch),
                new XAttribute("queryBinding", "xslt2"),
                new XElement(
                    Sch + "title",
                    "Özel belge doğrulama kuralları"));

            // 2. Kurallarda kullanılan namespace tanımlarını birleştir.
            var namespaces = new Dictionary<string, string>(
                StringComparer.Ordinal);

            foreach (var rule in rules)
            {
                foreach (var item in rule.Namespaces)
                {
                    if (namespaces.TryGetValue(item.Key, out var existingUri))
                    {
                        if (!string.Equals(
                            existingUri,
                            item.Value,
                            StringComparison.Ordinal))
                        {
                            throw new InvalidOperationException(
                                $"'{item.Key}' namespace öneki farklı adreslerle " +
                                "tanımlanmış. Birlikte çalışacak kurallarda " +
                                "aynı önek aynı adresi göstermelidir.");
                        }

                        continue;
                    }

                    namespaces.Add(item.Key, item.Value);
                }
            }

            foreach (var item in namespaces)
            {
                schema.Add(
                    new XElement(
                        Sch + "ns",
                        new XAttribute("prefix", item.Key),
                        new XAttribute("uri", item.Value)));
            }

            // 3. Her özel kural için ayrı bir pattern oluştur.
            var addedIds = new HashSet<Guid>();

            foreach (var rule in rules)
            {
                if (rule.Id == Guid.Empty || !addedIds.Add(rule.Id))
                {
                    throw new InvalidOperationException(
                        "Kural kimlikleri boş olamaz ve tekrarlanamaz.");
                }

                string identifier = rule.Id.ToString("N");

                var assertion = new XElement(
                    Sch + "assert",
                    new XAttribute("id", $"assert-{identifier}"),
                    new XAttribute("test", rule.Test),
                    rule.Message);

                var schematronRule = new XElement(
                    Sch + "rule",
                    new XAttribute("context", rule.Context),
                    assertion);

                var pattern = new XElement(
                    Sch + "pattern",
                    new XAttribute("id", $"pattern-{identifier}"),
                    schematronRule);

                schema.Add(pattern);
            }

            // 4. Hazırlanan XML belgesini döndür.
            return new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                schema);
        }
    }
}

//| Kod | Ne yapıyor ? |
//| ---| ---|
//| `schema` | Özel kuralların bulunacağı Schematron belgesini oluşturuyor. |
//| `namespaces` | Kuralların kullandığı `cbc`, `cac`, `inv` gibi önekleri topluyor. |
//| `rule.Context` ve `rule.Test` | Kontrolün nerede ve hangi koşulla yapılacağını XML’e yazıyor. |
//| `return new XDocument(...)` | Oluşturulan XML’i bellekte geri veriyor; henüz dosyaya kaydetmiyor. |
