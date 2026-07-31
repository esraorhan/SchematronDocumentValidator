using Saxon.Api;
using SchematronDocumentValidator.Infrastructure.Saxon.Compilation;
using SchematronDocumentValidator.Infrastructure.Saxon.Configuration;
using SchematronDocumentValidator.Infrastructure.Saxon.Resolution;
using SchematronDocumentValidator.Infrastructure.Saxon.Transformation;

try
{
    // 1. Sabit klasör ve dosya adlarını tanımlar.
    var options = new SchematronResourceOptions();

    // 2. Göreli yolları çalışan uygulamanın fiziksel yollarına çevirir.
    var pathResolver =
        new SchematronResourcePathResolver(options);

    SchematronResourcePaths resourcePaths =pathResolver.Resolve();

    // 3. Gerekli kaynak dosyaların varlığını kontrol eder.
    var resourceValidator =
        new SchematronResourceValidator();

    // 4. XSLT dosyalarını Saxon ile derleyip çalıştırır.
    var xsltTransformer =
        new SaxonXsltTransformer();

    // 5. Üç Schematron derleme aşamasını sırayla yönetir.

    var validatorPostProcessor =
    new GeneratedValidatorPostProcessor();

    var compilationPipeline =
        new SchematronCompilationPipeline(
            xsltTransformer,
            resourcePaths,
            resourceValidator,
            validatorPostProcessor);
  

    SchematronCompilationArtifacts artifacts = compilationPipeline.Compile();

    Console.WriteLine("Schematron derleme işlemi tamamlandı.");
    Console.WriteLine();

    Console.WriteLine($"Include çıktısı: {artifacts.IncludedSchematronPath}");

    Console.WriteLine($"Abstract çıktısı: {artifacts.ExpandedSchematronPath}");

    Console.WriteLine($"Validator XSLT: {artifacts.GeneratedValidatorXsltPath}");
}
catch (SaxonApiException exception)
{
    Console.WriteLine("Saxon işlemi başarısız oldu.");
    Console.WriteLine();

    Console.WriteLine($"Mesaj      : {exception.Message}");
    Console.WriteLine($"Hata kodu  : {exception.ErrorCode}");
    Console.WriteLine($"XSL modülü : {exception.ModuleUri}");
    Console.WriteLine($"Satır      : {exception.LineNumber}");
    Console.WriteLine($"Sütun      : {exception.ColumnNumber}");
}
catch (Exception exception)
{
    Console.WriteLine("Schematron derleme işlemi başarısız oldu.");
    Console.WriteLine();
    Console.WriteLine(exception);
}
