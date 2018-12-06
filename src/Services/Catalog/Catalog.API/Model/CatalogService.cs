using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.eShopOnContainers.Services.Catalog.API.Model;
using Microsoft.Extensions.Caching.Distributed;

namespace Catalog.API.Model
{
    public class CatalogService : ICatalogService
    {
        private readonly CatalogItemRepository _itemRepository;
        private readonly IRepository<CatalogType> _typeRepository;
        private readonly IRepository<CatalogBrand> _brandRepository;

        public CatalogService(IDistributedCache cache, ICatalogItemRepository itemRepository, IRepository<CatalogType> typeRepository,
           IRepository<CatalogBrand> brandRepository)
        {
            _itemRepository = itemRepository as CatalogItemRepository;
            _typeRepository = typeRepository;
            _brandRepository = brandRepository;
        }

        public async Task<CatalogItem> AddAsync(CatalogItem entity, bool saveChanges = true)
        {            
            return await _itemRepository.AddAsync(entity, saveChanges);
        }

        public async Task<CatalogItem> UpdateAsync(CatalogItem entity, bool saveChanges)
        {
            return await _itemRepository.UpdateAsync(entity, saveChanges);
        }

        public async Task<bool?> DeleteAsync(int id, bool saveChanges = true)
        {
            List<CatalogItem> totalCatalogItems = (await _itemRepository.ListAllAsync());
            var item = totalCatalogItems.SingleOrDefault(x => x.Id == id);
            if (item == null)
            {
                return null;
            }
            return await _itemRepository.DeleteAsync(id, saveChanges);
        }

        public async Task<CatalogItem> GetSingleBySpecAsync(ISpecification<CatalogItem> spec)
        {
            return await _itemRepository.GetSingleBySpecAsync(spec);
        }

        public async Task<IEnumerable<CatalogItem>> ListAllAsync()
        {
            return (await _itemRepository.ListAllAsync());
        }

        public async Task<IEnumerable<CatalogItem>> ListAsync(ISpecification<CatalogItem> spec)
        {
            return await _itemRepository.ListAsync(spec);
        }

        public async Task<IEnumerable<CatalogItem>> ListAsync(string ids, ISpecification<CatalogItem> spec)
        {
            if (!string.IsNullOrEmpty(ids))
            {
                var numIds = ids.Split(',').Select(id => (Ok: int.TryParse(id, out int x), Value: x));
                var idsToSelect = numIds.Select(id => id.Value);

                return (await ListAsync(spec)).Where(ci => idsToSelect.Contains(ci.Id)).ToList();
            }

            return (await ListAsync(spec));
        }

        public async Task<IEnumerable<CatalogType>> ListAllCatalogTypeAsync()
        {
            return (await _typeRepository.ListAllAsync());
        }

        public async Task<IEnumerable<CatalogBrand>> ListAllCatalogBrandAsync()
        {
            return (await _brandRepository.ListAllAsync());
        }
    }
}
