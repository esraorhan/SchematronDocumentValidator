using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchematronDocumentValidator.Core.Models
{
    public sealed class CustomSchematronRule
    {
        public Guid Id { get; init; } = Guid.NewGuid();

        public required string Code { get; init; }

        public required string Name { get; init; }

        public string? Description { get; init; }

        public required CustomSchematronRuleScope Scope { get; init; }

        // Kuralda kullanılan XPath önekleri ve namespace adresleri.
        public IReadOnlyDictionary<string, string> Namespaces { get; init; }
            = new Dictionary<string, string>();

        // Kontrolün uygulanacağı XML düğümü.
        public required string Context { get; init; }

        // True olması gereken XPath koşulu.
        public required string Test { get; init; }

        public required string Message { get; init; }

        public bool IsActive { get; init; } = true;
    }
}


