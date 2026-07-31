using SchematronDocumentValidator.Application.Abstractions;
using SchematronDocumentValidator.Core.Abstractions;
using SchematronDocumentValidator.Core.Enums;
using SchematronDocumentValidator.Core.Models;

namespace SchematronDocumentValidator.Application.Services;

public sealed class DocumentValidationService : IDocumentValidationService
{
    private readonly ISchematronValidationEngine _validationEngine;

    public DocumentValidationService(ISchematronValidationEngine validationEngine)
    {
        _validationEngine = validationEngine?? throw new ArgumentNullException(nameof(validationEngine));
    }

    public Task<SchematronValidationResult> ValidateAsync(string xmlPath, SchematronDocumentType documentType,
        CancellationToken cancellationToken = default)
    {
        ValidateInput(xmlPath, documentType);

        return _validationEngine.ValidateAsync(xmlPath, documentType, cancellationToken);
    }

    private static void ValidateInput(string xmlPath,SchematronDocumentType documentType)
    {
        if (string.IsNullOrWhiteSpace(xmlPath))
        {
            throw new ArgumentException( "XML dosya yolu boş olamaz.", nameof(xmlPath));
        }

        if (!File.Exists(xmlPath))
        {
            throw new FileNotFoundException(
                "Doğrulanacak XML dosyası bulunamadı.",
                xmlPath);
        }

        if (!Enum.IsDefined(typeof(SchematronDocumentType), documentType))
        {
            throw new ArgumentOutOfRangeException(nameof(documentType),documentType,"Desteklenmeyen belge türü.");
        }
    }
}