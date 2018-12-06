using System.Threading.Tasks;
using System.Collections.Generic;

namespace Catalog.API.Model
{
    public interface IService<T>
    {
        // CRUD
        Task<T> AddAsync(T entity, bool saveChanges = true);
        Task<T> UpdateAsync(T entity, bool saveChanges);
        Task<bool?> DeleteAsync(int id, bool saveChanges = true);

        // R of CRUD
        Task<IEnumerable<T>> ListAllAsync();
        Task<T> GetSingleBySpecAsync(ISpecification<T> spec);
        Task<IEnumerable<T>> ListAsync(ISpecification<T> spec);
    }
}
