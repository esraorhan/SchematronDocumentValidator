using SchematronDocumentValidator.Core.Enums;
using SchematronDocumentValidator.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchematronDocumentValidator.Application.Abstractions
{
    public interface IDocumentValidationService
    {
        Task<SchematronValidationResult> ValidateAsync(
            string xmlPath,
            SchematronDocumentType documentType,
            CancellationToken cancellationToken = default);
    }


//    IDocumentValidationService
//→ Uygulamanın sunduğu kullanım senaryosu

//ISchematronValidationEngine
//→ Schematron doğrulamasını gerçekleştiren teknik motor
}
