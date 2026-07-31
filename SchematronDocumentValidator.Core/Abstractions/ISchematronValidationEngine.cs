using SchematronDocumentValidator.Core.Enums;
using SchematronDocumentValidator.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchematronDocumentValidator.Core.Abstractions
{
    public interface ISchematronValidationEngine
    {
        Task<SchematronValidationResult> ValidateAsync(string xmlPath,SchematronDocumentType documentType,CancellationToken cancellationToken = default);
    }
}
