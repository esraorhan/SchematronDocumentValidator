using Microsoft.AspNetCore.Mvc;
using SchematronDocumentValidator.Application.Abstractions;
using SchematronDocumentValidator.Core.Enums;
using SchematronDocumentValidator.Web.Models;
using System.Diagnostics;
using System.Xml;

namespace SchematronDocumentValidator.Web.Controllers;
public sealed class HomeController(IDocumentValidationService service, ILogger<HomeController> logger) : Controller
{
    public IActionResult Index() => View(new ValidationPageModel());
    public IActionResult Error() => View("Index", new ValidationPageModel { Error = "Ýþlem tamamlanamadý. Lütfen tekrar deneyin." });

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 11 * 1024 * 1024)]
    public async Task<IActionResult> Validate(IFormFile? document, CancellationToken cancellationToken)
    {
        var model = new ValidationPageModel { FileName = Path.GetFileName(document?.FileName) };
        if (document is null || document.Length == 0 || document.Length > 10 * 1024 * 1024 ||
            !string.Equals(Path.GetExtension(document.FileName), ".xml", StringComparison.OrdinalIgnoreCase))
        {
            model.Error = "En fazla 10 MB boyutunda, boþ olmayan bir XML dosyasý seçin.";
            return View("Index", model);
        }
        var folder = Path.Combine(Path.GetTempPath(), "schematron-upload", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "document.xml");
        try
        {
            await using (var output = System.IO.File.Create(path))
                await document.CopyToAsync(output, cancellationToken);
            model.Result = await service.ValidateAsync(path, SchematronDocumentType.Invoice, cancellationToken);
        }
        catch (XmlException ex)
        {
            model.Error = $"XML okunamadý. Satýr {ex.LineNumber}, sütun {ex.LinePosition}. XML yapýsýný kontrol edin; DTD desteklenmez.";
        }
        catch (NotSupportedException ex) { model.Error = ex.Message; }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Schematron doðrulamasý tamamlanamadý.");
            model.Error = "Doðrulama motoru çalýþtýrýlamadý. Belge için geçerlilik sonucu üretilmedi. Teknik ayrýntýlar uygulama günlüðünde.";
        }
        finally { Directory.Delete(folder, true); }
        return View("Index", model);
    }
}