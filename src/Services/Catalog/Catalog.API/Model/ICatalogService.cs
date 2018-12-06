using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.eShopOnContainers.Services.Catalog.API.Model;

namespace Catalog.API.Model
{
    public interface ICatalogService : IService<CatalogItem>
    {
        Task<IEnumerable<CatalogItem>> ListAsync(string ids, ISpecification<CatalogItem> spec);
        Task<IEnumerable<CatalogType>> ListAllCatalogTypeAsync();
        Task<IEnumerable<CatalogBrand>> ListAllCatalogBrandAsync();
    }
}
