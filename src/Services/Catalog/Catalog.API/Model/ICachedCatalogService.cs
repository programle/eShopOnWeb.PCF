using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.eShopOnContainers.Services.Catalog.API.Model;

namespace Catalog.API.Model
{
    public interface ICachedCatalogService : IService<CatalogItem>
    {
        Task<IEnumerable<CatalogItem>> ListAsync(string ids, ISpecification<CatalogItem> spec, CacheCatalogFilter filter);
        Task<CatalogItem> GetSingleBySpecAsync(ISpecification<CatalogItem> spec, CacheCatalogFilter filter);
        Task<IEnumerable<CatalogItem>> ListAsync(ISpecification<CatalogItem> spec, CacheCatalogFilter filter);
    }
}
