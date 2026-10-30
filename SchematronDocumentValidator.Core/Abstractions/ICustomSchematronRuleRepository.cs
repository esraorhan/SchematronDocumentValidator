using SchematronDocumentValidator.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchematronDocumentValidator.Core.Abstractions
{
    public interface ICustomSchematronRuleRepository
    {
        Task<IReadOnlyList<CustomSchematronRule>> GetAllAsync( CancellationToken cancellationToken = default);

        Task SaveAsync( CustomSchematronRule rule, CancellationToken cancellationToken = default);

        Task DeleteAsync( Guid id,  CancellationToken cancellationToken = default);
    }
}
