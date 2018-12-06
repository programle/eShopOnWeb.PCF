using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.eShopOnContainers.Services.Catalog.API.Model;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

using Catalog.API.Extensions;
using System;

namespace Catalog.API.Model
{
    public class CachedCatalogService : ICachedCatalogService
    {
        private readonly IDistributedCache _cache;

        private enum TotalCachedKeys
        {
            TOTAL_CATALOG_ITEMS,
            TOTAL_CATALOG_TYPES,
            TOTAL_CATALOG_BRANDS
        }

        private readonly ILogger<CachedCatalogService> _logger;
        private readonly CatalogService _catalogService;        

        public CachedCatalogService(IDistributedCache cache, CatalogService catalogService, ILogger<CachedCatalogService> logger)
        {
            _cache = cache;
            _catalogService = catalogService;
            _logger = logger;
        }

        private async Task<IEnumerable<CatalogItem>> ListFilteredAsync(ISpecification<CatalogItem> spec, CacheCatalogFilter filter, string ids = null)
        {
            if (filter == null)
                return null;

            var items = await _cache.TryGetAsync<List<CatalogItem>>(filter.Key);
            if (items == null)
            {
                items = (await _catalogService.ListAsync(spec)).ToList();

                if (filter.PageSize.HasValue)
                {
                    items = items.Skip(filter.PageSize.Value * (filter.PageIndex.HasValue ? filter.PageIndex.Value : 1)).Take(filter.PageSize.Value).ToList();
                }
                await _cache.TrySetAsync(filter.Key, items);
            }
            return items;
        }

        private async Task<bool> ResetTotalAsync()
        {
            return await _cache.TryResetAsync(nameof(TotalCachedKeys.TOTAL_CATALOG_ITEMS), await _catalogService.ListAllAsync());
        }

        private async Task<bool> RemovePartialAsync()
        {
            return await _cache.RemovePartialAsync<List<CatalogItem>>(Enum.GetNames(typeof(TotalCachedKeys)).ToList());
        }

        public async Task<CatalogItem> AddAsync(CatalogItem entity, bool saveChanges = true)
        {
            var item = await _catalogService.AddAsync(entity, saveChanges);
            var result = await RemovePartialAsync();
            result = await ResetTotalAsync();
            return item;
        }

        public async Task<CatalogItem> UpdateAsync(CatalogItem entity, bool saveChanges)
        {
            var item = await _catalogService.UpdateAsync(entity, saveChanges);
            var result = await RemovePartialAsync();
            result = await ResetTotalAsync();
            return item;
        }

        public async Task<bool?> DeleteAsync(int id, bool saveChanges = true)
        {
            await _catalogService.DeleteAsync(id, saveChanges);
            var result = await RemovePartialAsync();
            result = await ResetTotalAsync();
            return result;
        }

        public async Task<IEnumerable<CatalogItem>> ListAllAsync()
        {
            var catalogItems = await _cache.TryGetAsync<List<CatalogItem>>(nameof(TotalCachedKeys.TOTAL_CATALOG_ITEMS));
            if (catalogItems == null)
            {
                catalogItems = (await _catalogService.ListAllAsync()).ToList();
                await _cache.TrySetAsync(nameof(TotalCachedKeys.TOTAL_CATALOG_ITEMS), catalogItems);
            }
            return catalogItems;
        }

        public async Task<CatalogItem> GetSingleBySpecAsync(ISpecification<CatalogItem> spec, CacheCatalogFilter filter)
        {
            if (filter == null)
                return null;

            var item = await _cache.TryGetAsync<CatalogItem>(filter.Key);
            if (item == null)
            {
                item = (await _catalogService.GetSingleBySpecAsync(spec));
                await _cache.TrySetAsync(filter.Key, item);
            }
            return item;
        }

        public async Task<IEnumerable<CatalogItem>> ListAsync(ISpecification<CatalogItem> spec, CacheCatalogFilter filter)
        {
            return await ListFilteredAsync(spec, filter);
        }        

        public async Task<IEnumerable<CatalogItem>> ListAsync(string ids, ISpecification<CatalogItem> spec, CacheCatalogFilter filter)
        {
            return await ListFilteredAsync(spec, filter, ids);
        }

        public async Task<IEnumerable<CatalogType>> ListAllCatalogTypeAsync()
        {
            var catalogTypes = await _cache.TryGetAsync<List<CatalogType>>(nameof(TotalCachedKeys.TOTAL_CATALOG_TYPES));
            if (catalogTypes == null)
            {
                catalogTypes = (await _catalogService.ListAllCatalogTypeAsync()).ToList();
                await _cache.TrySetAsync(nameof(TotalCachedKeys.TOTAL_CATALOG_TYPES), catalogTypes);
            }

            return catalogTypes;
        }

        public async Task<IEnumerable<CatalogBrand>> ListAllCatalogBrandAsync()
        {
            var catalogBrands = await _cache.TryGetAsync<List<CatalogBrand>>(nameof(TotalCachedKeys.TOTAL_CATALOG_BRANDS));
            if (catalogBrands == null)
            {
                catalogBrands = (await _catalogService.ListAllCatalogBrandAsync()).ToList();
                await _cache.TrySetAsync(nameof(TotalCachedKeys.TOTAL_CATALOG_BRANDS), catalogBrands);
            }

            return catalogBrands;
        }

        public async Task<CatalogItem> GetSingleBySpecAsync(ISpecification<CatalogItem> spec)
        {
            return (await _catalogService.GetSingleBySpecAsync(spec));
        }

        public async Task<IEnumerable<CatalogItem>> ListAsync(ISpecification<CatalogItem> spec)
        {
            return (await _catalogService.ListAsync(spec)).ToList();
        }
    }
}
