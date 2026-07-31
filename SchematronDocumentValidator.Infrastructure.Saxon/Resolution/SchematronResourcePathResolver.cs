using SchematronDocumentValidator.Infrastructure.Saxon.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchematronDocumentValidator.Infrastructure.Saxon.Resolution
{


    public sealed class SchematronResourcePathResolver
    {
        private readonly SchematronResourceOptions _options;

        public SchematronResourcePathResolver(
            SchematronResourceOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            _options = options;
        }

        public SchematronResourcePaths Resolve()
        {
            string resourceRootDirectory = Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    _options.ResourcesDirectory));

            string compilerXsltDirectory = Path.Combine(
                resourceRootDirectory,
                _options.CompilerXsltDirectory);

            string gibEFaturaPackageDirectory = Path.Combine(
                resourceRootDirectory,
                _options.GibEFaturaPackageDirectory);

            string generatedDirectory = Path.Combine(
                resourceRootDirectory,
                _options.GeneratedDirectory);

            return new SchematronResourcePaths
            {
                MainSchematronPath = Path.Combine(gibEFaturaPackageDirectory, _options.MainSchematronFileName),

                IncludeXsltPath = Path.Combine(compilerXsltDirectory, _options.IncludeXsltFileName),

                AbstractExpandXsltPath = Path.Combine(compilerXsltDirectory, _options.AbstractExpandXsltFileName),

                MessageXsltPath = Path.Combine( compilerXsltDirectory,
                    _options.MessageXsltFileName),

                GeneratedDirectoryPath = generatedDirectory
            };
        }
    }
}

