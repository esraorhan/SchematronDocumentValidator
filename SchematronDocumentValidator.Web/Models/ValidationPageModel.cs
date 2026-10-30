using SchematronDocumentValidator.Core.Models;
namespace SchematronDocumentValidator.Web.Models;
public sealed class ValidationPageModel
{
    public string? FileName { get; set; }
    public string? Error { get; set; }
    public SchematronValidationResult? Result { get; set; }
}