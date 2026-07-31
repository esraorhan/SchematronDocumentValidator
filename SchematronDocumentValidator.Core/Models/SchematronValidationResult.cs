using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchematronDocumentValidator.Core.Models
{
    public sealed class SchematronValidationResult
    {
        public required IReadOnlyList<SchematronViolation> Violations { get; init; }

        public bool IsValid => Violations.Count == 0;

        public int ViolationCount => Violations.Count;

        public TimeSpan Duration { get; init; }

        public string? RawSvrl { get; init; }
    }
}
