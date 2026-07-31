using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchematronDocumentValidator.Core.Models
{
    public sealed class SchematronViolation
    {
        public string? RuleId { get; init; }

        public string? Test { get; init; }

        public string? Location { get; init; }

        public required string Message { get; init; }

        public string? Role { get; init; }

        public string? Flag { get; init; }
    }
}
