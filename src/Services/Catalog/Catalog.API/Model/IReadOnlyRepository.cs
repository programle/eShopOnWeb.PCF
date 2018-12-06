using System.Threading.Tasks;
using System.Collections.Generic;

namespace Catalog.API.Model
{
    public interface IReadOnlyRepository<T>
    {
        Task<List<T>> ListAllAsync();
    }
}
