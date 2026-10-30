using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Saxon.Api;
using SchematronDocumentValidator.Infrastructure.Saxon.Resolution;
using SchematronDocumentValidator.Infrastructure.Saxon.Transformation;

namespace SchematronDocumentValidator.Infrastructure.Saxon.Compilation
{
    public sealed class SchematronCompilationPipeline
    {
        private const string IncludedFileName = "01-included-schematron.xml";

        private const string ExpandedFileName = "02-expanded-schematron.xml";

        private const string GeneratedValidatorFileName = "03-efatura_schematron.xsl";

        private readonly SaxonXsltTransformer _xsltTransformer;
        private readonly SchematronResourcePaths _resourcePaths;
        private readonly SchematronResourceValidator _resourceValidator;
        private readonly GeneratedValidatorPostProcessor _validatorPostProcessor;
        public SchematronCompilationPipeline(
            SaxonXsltTransformer xsltTransformer,
            SchematronResourcePaths resourcePaths,
            SchematronResourceValidator resourceValidator,
            GeneratedValidatorPostProcessor validatorPostProcessor)
        {
            _xsltTransformer = xsltTransformer
                ?? throw new ArgumentNullException(
                    nameof(xsltTransformer));

            _resourcePaths = resourcePaths
                ?? throw new ArgumentNullException(
                    nameof(resourcePaths));

            _resourceValidator = resourceValidator
                ?? throw new ArgumentNullException(
                    nameof(resourceValidator));

            _validatorPostProcessor = validatorPostProcessor
                ?? throw new ArgumentNullException(
                    nameof(validatorPostProcessor));
        }

        public SchematronCompilationArtifacts Compile(
          string generatedValidatorFileName = GeneratedValidatorFileName)
        {
            if (string.IsNullOrWhiteSpace(generatedValidatorFileName) ||
                generatedValidatorFileName.IndexOfAny(
                    Path.GetInvalidFileNameChars()) >= 0 ||
                generatedValidatorFileName.Contains('/') ||
                generatedValidatorFileName.Contains('\\') ||
                generatedValidatorFileName is "." or "..")
            {
                throw new ArgumentException(
                    "Çıktı adı klasör yolu içermeyen geçerli bir dosya adı olmalıdır.",
                    nameof(generatedValidatorFileName));
            }

            _resourceValidator.Validate(_resourcePaths);

            SchematronCompilationArtifacts artifacts =
                CreateArtifacts(generatedValidatorFileName);

            // Derleme araçları birer XSLT programıdır.
            XsltExecutable includeExecutable =
                _xsltTransformer.CompileStylesheet(
                    _resourcePaths.IncludeXsltPath);

            XsltExecutable abstractExpandExecutable =
                _xsltTransformer.CompileStylesheet(
                    _resourcePaths.AbstractExpandXsltPath);

            XsltExecutable messageExecutable =
                _xsltTransformer.CompileStylesheet(
                    _resourcePaths.MessageXsltPath);

            // 1. GİB Schematron paketindeki include bağlantılarını çözer.
            _xsltTransformer.TransformToFile(
                _resourcePaths.MainSchematronPath,
                includeExecutable,
                artifacts.IncludedSchematronPath);

            // 2. Abstract pattern ve rule tanımlarını genişletir.
            _xsltTransformer.TransformToFile(
                artifacts.IncludedSchematronPath,
                abstractExpandExecutable,
                artifacts.ExpandedSchematronPath);

            // 3. Gerçek XML belgelerine uygulanacak validator XSLT'yi üretir.

            var validatorGenerationParameters = new Dictionary<string, string>
            {
                ["attributes"] = "true"
            };
            _xsltTransformer.TransformToFile(
                artifacts.ExpandedSchematronPath,
                messageExecutable,
                artifacts.GeneratedValidatorXsltPath,
                validatorGenerationParameters);

            _validatorPostProcessor.EnsureAttributeTraversal(
                             artifacts.GeneratedValidatorXsltPath);
            return artifacts;
        }

        private SchematronCompilationArtifacts CreateArtifacts(string outputFileName)
        {
            string generatedDirectory =
                _resourcePaths.GeneratedDirectoryPath;

            Directory.CreateDirectory(generatedDirectory);

            return new SchematronCompilationArtifacts
            {
                IncludedSchematronPath = Path.Combine(
                    generatedDirectory,
                    IncludedFileName),

                ExpandedSchematronPath = Path.Combine(
                    generatedDirectory,
                    ExpandedFileName),

                GeneratedValidatorXsltPath = Path.Combine(
                    generatedDirectory,
                    outputFileName)
            };
        }
    }
}
