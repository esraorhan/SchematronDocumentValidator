using Saxon.Api;
using SchematronDocumentValidator.Core.Abstractions;
using SchematronDocumentValidator.Core.Enums;
using SchematronDocumentValidator.Core.Models;
using SchematronDocumentValidator.Infrastructure.Saxon.Compilation;
using SchematronDocumentValidator.Infrastructure.Saxon.Configuration;
using SchematronDocumentValidator.Infrastructure.Saxon.Resolution;
using SchematronDocumentValidator.Infrastructure.Saxon.Transformation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SchematronDocumentValidator.Infrastructure.Saxon.Validation
{
    public sealed class SaxonSchematronValidationEngine : ISchematronValidationEngine
    {
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly SaxonXsltTransformer _transformer = new();
        private XsltExecutable? _validator;

        public async Task<SchematronValidationResult> ValidateAsync(string xmlPath,
            SchematronDocumentType documentType, CancellationToken cancellationToken = default)
        {
            if (documentType != SchematronDocumentType.Invoice)
                throw new NotSupportedException("Bu ekran şu anda yalnızca UBL e-Fatura XML belgelerini destekliyor.");
            // Reject DTDs and unsupported roots before Saxon reads the same private file.
            using (var reader = XmlReader.Create(xmlPath, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = 20 * 1024 * 1024
            }))
            {
                reader.MoveToContent();
                if (reader.LocalName != "Invoice" || reader.NamespaceURI != "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2")
                    throw new NotSupportedException("Desteklenmeyen belge: UBL Invoice namespace'ine sahip bir e-Fatura XML dosyası yükleyin.");
                while (reader.Read()) cancellationToken.ThrowIfCancellationRequested();
            }
            await _gate.WaitAsync(cancellationToken);
            var output = Path.Combine(Path.GetTempPath(), $"schematron-result-{Guid.NewGuid():N}.txt");
            try
            {
                var watch = Stopwatch.StartNew();
                if (_validator is null)
                {
                    var paths = new SchematronResourcePathResolver(new SchematronResourceOptions()).Resolve();
                    var pipeline = new SchematronCompilationPipeline(_transformer, paths,
                        new SchematronResourceValidator(), new GeneratedValidatorPostProcessor());
                    _validator = _transformer.CompileStylesheet(pipeline.Compile().GeneratedValidatorXsltPath);
                }
                cancellationToken.ThrowIfCancellationRequested();
                _transformer.TransformToFile(xmlPath, _validator, output);
                cancellationToken.ThrowIfCancellationRequested();
                var text = await File.ReadAllTextAsync(output, cancellationToken);
                // The existing plain-text compiler emits one message per line; it provides no rule metadata.
                var violations = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(message => new SchematronViolation { Message = message }).ToArray();
                watch.Stop();
                return new SchematronValidationResult { Violations = violations, Duration = watch.Elapsed };
            }
            finally
            {
                try { if (File.Exists(output)) File.Delete(output); }
                finally { _gate.Release(); }
            }
        }
    }
}
