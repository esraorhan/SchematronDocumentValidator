using SchematronDocumentValidator.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchematronDocumentValidator.Core.Models
{
    public sealed class CustomSchematronRuleScope  //Bu sınıf, kuralın hangi belgelerde çalışacağını belirleyecek:
    {
        // En az bir belge türü seçilmesi zorunlu olacak.
        // Boş liste "bütün belgeler" anlamına gelmeyecek.
        public IReadOnlyList<SchematronDocumentType> DocumentTypes { get; init; }
            = Array.Empty<SchematronDocumentType>();

        // Boş liste: seçili belge türlerinin tüm profilleri.
        // Dolu liste: yalnızca belirtilen profiller.
        public IReadOnlyList<string> ProfileIds { get; init; }
            = Array.Empty<string>();
    }
}
