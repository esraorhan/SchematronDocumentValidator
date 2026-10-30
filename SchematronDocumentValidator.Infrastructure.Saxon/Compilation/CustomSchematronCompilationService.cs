using SchematronDocumentValidator.Core.Models;
using SchematronDocumentValidator.Infrastructure.Saxon.Configuration;
using SchematronDocumentValidator.Infrastructure.Saxon.Resolution;
using SchematronDocumentValidator.Infrastructure.Saxon.Transformation;

namespace SchematronDocumentValidator.Infrastructure.Saxon.Compilation;

public sealed class CustomSchematronCompilationService
{
    public SchematronCompilationArtifacts Compile( IReadOnlyCollection<CustomSchematronRule> rules,string workingDirectory)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);

        string fullWorkingDirectory =Path.GetFullPath(workingDirectory);

        Directory.CreateDirectory(fullWorkingDirectory);

        // 1. Kuralları Schematron XML'ine çevir.jsondan 
        var documentBuilder = new CustomSchematronDocumentBuilder();

        var schematronDocument = documentBuilder.Build(rules);

        // 2. Özel Schematron kaynağını ayrı dosyaya kaydet.
        string sourcePath = Path.Combine( fullWorkingDirectory, "custom-rules.sch");

        schematronDocument.Save(sourcePath);

        // 3. Ortak derleyici XSL dosyalarının yollarını bul.
        var resourceResolver = new SchematronResourcePathResolver(
            new SchematronResourceOptions());

        SchematronResourcePaths commonResources = resourceResolver.Resolve();

        // 4. Bu işlemde kullanılacak kaynak ve çıktı yollarını tanımla.
        var customResources = new SchematronResourcePaths
        {
            MainSchematronPath = sourcePath,

            IncludeXsltPath = commonResources.IncludeXsltPath,

            AbstractExpandXsltPath = commonResources.AbstractExpandXsltPath,

            MessageXsltPath = commonResources.MessageXsltPath,

            GeneratedDirectoryPath = Path.Combine( fullWorkingDirectory, "Generated")
        };

        // 5. Mevcut derleme hattını özel kaynakla çalıştır.
        var transformer = new SaxonXsltTransformer();

        var pipeline = new SchematronCompilationPipeline( transformer, customResources, new SchematronResourceValidator(), new GeneratedValidatorPostProcessor());

        SchematronCompilationArtifacts artifacts =  pipeline.Compile("03-custom-schematron.xsl");

        // 6. Üretilen XSL'yi Saxon'a derlet.
        // Statik XPath hataları varsa burada exception oluşur.
        transformer.CompileStylesheet(  artifacts.GeneratedValidatorXsltPath);

        return artifacts;
    }
}
