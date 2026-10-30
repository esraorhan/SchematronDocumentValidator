using System.Diagnostics;
using SchematronDocumentValidator.Core.Models;
using SchematronDocumentValidator.Infrastructure.Saxon.Transformation;

namespace SchematronDocumentValidator.Infrastructure.Saxon.Validation;

public sealed class CustomSchematronTestRunner
{
    public async Task<SchematronValidationResult> ValidateAsync(
        string xmlPath,
        string xsltPath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(xsltPath))
        {
            throw new FileNotFoundException(
                "Test için seçilen özel XSL bulunamadı.",
                xsltPath);
        }

        string outputPath = Path.Combine(
            Path.GetTempPath(),
            $"custom-validation-{Guid.NewGuid():N}.txt");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var transformer = new SaxonXsltTransformer();

            // Hazır XSL'yi Saxon'un çalıştırabileceği hâle getir.
            var executable = transformer.CompileStylesheet(xsltPath);

            cancellationToken.ThrowIfCancellationRequested();

            // Aynı fatura XML'ini özel XSL ile kontrol et.
            transformer.TransformToFile(
                xmlPath,
                executable,
                outputPath);

            string output = await File.ReadAllTextAsync(
                outputPath,
                cancellationToken);

            // Mevcut üreticimiz düz metin hata çıktısı veriyor.
            var violations = output
                .Split(
                    new[] { '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .Select(message => new SchematronViolation
                {
                    Message = message
                })
                .ToArray();

            stopwatch.Stop();

            return new SchematronValidationResult
            {
                Violations = violations,
                Duration = stopwatch.Elapsed
            };
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }
}
