using System;
using System.Collections.Generic;
using System.Text;

namespace Wcs.Domain.Field
{
    public interface IFieldTagRepository
    {
        Task<IReadOnlyList<FieldTag>> GetAllAsync(CancellationToken ct = default);
    }
}
