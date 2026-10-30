using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SchematronDocumentValidator.Core.Enums;
using SchematronDocumentValidator.Core.Models;
namespace SchematronDocumentValidator.Application.Validation
{

    public sealed class CustomSchematronRuleValidator
    {
        //kural tanımındaki eksikleri kontrol ediyor
        public IReadOnlyList<string> Validate(CustomSchematronRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(rule.Code))
            {
                errors.Add("Kural kodu zorunludur.");
            }

            if (string.IsNullOrWhiteSpace(rule.Name))
            {
                errors.Add("Kural adı zorunludur.");
            }

            if (string.IsNullOrWhiteSpace(rule.Context))
            {
                errors.Add("Kuralın uygulanacağı alan (Context) zorunludur.");
            }

            if (string.IsNullOrWhiteSpace(rule.Test))
            {
                errors.Add("Kontrol koşulu (Test) zorunludur.");
            }

            if (string.IsNullOrWhiteSpace(rule.Message))
            {
                errors.Add("Hata mesajı zorunludur.");
            }

            ValidateScope(rule, errors);
            ValidateNamespaces(rule, errors);

            return errors;
        }

        private static void ValidateScope( CustomSchematronRule rule,List<string> errors)
        {
            if (rule.Scope is null)
            {
                errors.Add("Kural kapsamı zorunludur.");
                return;
            }

            var documentTypes = rule.Scope.DocumentTypes;

            if (documentTypes is null || documentTypes.Count == 0)
            {
                errors.Add("En az bir belge türü seçilmelidir.");
            }
            else
            {
                foreach (var documentType in documentTypes)
                {
                    if (!Enum.IsDefined(
                        typeof(SchematronDocumentType),
                        documentType))
                    {
                        errors.Add(
                            $"Geçersiz belge türü: {(int)documentType}.");
                    }
                }
            }

            if (rule.Scope.ProfileIds is null)
            {
                errors.Add("Profil listesi null olamaz; tüm profiller için boş liste kullanın.");
            }
            else if (rule.Scope.ProfileIds.Any(string.IsNullOrWhiteSpace))
            {
                errors.Add("Profil filtresi boş bir değer içeremez.");
            }
        }

        private static void ValidateNamespaces( CustomSchematronRule rule,List<string> errors)
        {
            if (rule.Namespaces is null)
            {
                errors.Add("Namespace listesi null olamaz; gerekmiyorsa boş bırakın.");
                return;
            }

            foreach (var item in rule.Namespaces)
            {
                var prefix = item.Key;
                var namespaceUri = item.Value;

                if (string.IsNullOrWhiteSpace(prefix))
                {
                    errors.Add("Namespace öneki boş olamaz.");
                    continue;
                }

                try
                {
                    XmlConvert.VerifyNCName(prefix);
                }
                catch (XmlException)
                {
                    errors.Add($"Geçersiz namespace öneki: {prefix}.");
                    continue;
                }

                if (prefix.StartsWith(
                    "xml",
                    StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add(
                        $"'{prefix}' ayrılmış bir önektir; farklı bir önek kullanın.");
                }

                if (string.IsNullOrWhiteSpace(namespaceUri) || namespaceUri.Any(char.IsWhiteSpace) || !Uri.TryCreate( namespaceUri, UriKind.Absolute, out _))
                {
                    errors.Add( $"'{prefix}' için geçerli bir namespace adresi girin.");
                }
            }
        }
    }
}
