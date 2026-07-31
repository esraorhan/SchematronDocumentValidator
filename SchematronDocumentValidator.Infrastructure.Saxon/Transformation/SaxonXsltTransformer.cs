using Saxon.Api;
namespace SchematronDocumentValidator.Infrastructure.Saxon.Transformation
{
    public sealed class SaxonXsltTransformer
    {
        private readonly Processor _processor;
        private readonly XsltCompiler _compiler;

        public SaxonXsltTransformer()
        {
            _processor = new Processor();
            _compiler = _processor.NewXsltCompiler();
        }

        public XsltExecutable CompileStylesheet(string xsltPath)
        {
            EnsureFileExists(xsltPath, "XSLT dosyası");

            Uri xsltUri = CreateFileUri(xsltPath);

            return _compiler.Compile(xsltUri);
        }

        public void TransformToFile(
            string sourceXmlPath,
            XsltExecutable executable,
            string outputPath, IReadOnlyDictionary<string, string>? parameters = null)
        {
            EnsureFileExists(
                sourceXmlPath,
                "Kaynak XML dosyası");

            ArgumentNullException.ThrowIfNull(executable);

            string fullOutputPath =
                Path.GetFullPath(outputPath);

            string? outputDirectory =
                Path.GetDirectoryName(fullOutputPath);

            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            Uri sourceUri =
                CreateFileUri(sourceXmlPath);

            XsltTransformer transformer =executable.Load();

            if (parameters != null)
            {
                foreach (KeyValuePair<string, string> parameter in parameters)
                {
                    transformer.SetParameter(
                        new QName("", parameter.Key),
                        new XdmAtomicValue(parameter.Value));
                }
            }

            transformer.BaseOutputUri =
                CreateFileUri(fullOutputPath);

            using FileStream inputStream = new(
                sourceXmlPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

            using FileStream outputStream = new(
                fullOutputPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None);

            Serializer serializer =
                _processor.NewSerializer();

            serializer.OutputStream =
                outputStream;

            /*
             * Saxon'a ana XML belgesini ve bu belgenin fiziksel
             * konumunu birlikte veriyoruz.
             *
             * Böylece:
             *  - XML global context olur.
             *  - "/" ve root() çalışır.
             *  - Göreli dosya bağlantıları kaynak XML'in
             *    klasörüne göre çözülür.
             */
            transformer.SetInputStream(
                inputStream,
                sourceUri);

            transformer.Run(serializer);
        }

        private static Uri CreateFileUri(string filePath)
        {
            string fullPath = Path.GetFullPath(filePath);

            return new Uri(fullPath, UriKind.Absolute);
        }

        private static void EnsureFileExists(string filePath,string resourceName)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException(
                    $"{resourceName} yolu boş olamaz.",
                    nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(
                    $"{resourceName} bulunamadı. Yol: {filePath}",
                    filePath);
            }
        }
    }
}
