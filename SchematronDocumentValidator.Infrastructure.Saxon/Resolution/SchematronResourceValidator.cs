using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchematronDocumentValidator.Infrastructure.Saxon.Resolution
{

    public sealed class SchematronResourceValidator //Generated klasörünü oluşturcak
    {
        public void Validate(SchematronResourcePaths paths)
        {
            ArgumentNullException.ThrowIfNull(paths);

            EnsureFileExists(paths.MainSchematronPath,
                "GİB ana Schematron dosyası");

            EnsureFileExists(paths.IncludeXsltPath,
                "Include dönüşüm XSL dosyası");

            EnsureFileExists( paths.AbstractExpandXsltPath,
                "Abstract expansion XSL dosyası");

            EnsureFileExists(paths.MessageXsltPath,
                "Validator üretim XSL dosyası");

            Directory.CreateDirectory(paths.GeneratedDirectoryPath);
        }

        private static void EnsureFileExists(string filePath,string resourceName)
        {
            if (File.Exists(filePath))
            {
                return;
            }

            throw new FileNotFoundException(
                $"{resourceName} bulunamadı. Dosya yolu: {filePath}",
                filePath);
        }
    }
}
