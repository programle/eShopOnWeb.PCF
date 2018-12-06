using Microsoft.eShopOnContainers.Services.Catalog.API.Infrastructure;
using Microsoft.eShopOnContainers.Services.Catalog.API.Model;
using Microsoft.Extensions.Caching.Distributed;

namespace Catalog.API.Model
{
    public class CatalogItemRepository : EFRepository<CatalogItem>, ICatalogItemRepository
    {
        public CatalogItemRepository(CatalogContext dbContext) : base(dbContext)
        {
        }
    }
}
