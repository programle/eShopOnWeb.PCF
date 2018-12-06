using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Microsoft.eShopOnContainers.Services.Catalog.API.IntegrationEvents.Events;
using Microsoft.eShopOnContainers.Services.Catalog.API.Model;
using Microsoft.eShopOnContainers.Services.Catalog.API.ViewModel;

using Catalog.API.IntegrationEvents;
using Catalog.API.Model;
using Microsoft.Extensions.Caching.Distributed;

namespace Microsoft.eShopOnContainers.Services.Catalog.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class CatalogController : Controller
    {
        private readonly CatalogSettings _settings;        
        private readonly ICatalogIntegrationEventService _catalogIntegrationEventService;

        private readonly CachedCatalogService _catalogService;
        

        public CatalogController(ICachedCatalogService catalogService, IOptionsSnapshot<CatalogSettings> settings,
           ICatalogIntegrationEventService catalogIntegrationEventService)
        {            
            _catalogService = catalogService is CachedCatalogService ? (catalogService as CachedCatalogService) : null;
            _catalogIntegrationEventService = catalogIntegrationEventService ?? throw new ArgumentNullException(nameof(catalogIntegrationEventService));
            _settings = settings.Value;
        }

        // GET api/v1/[controller]/items[?pageSize=3&pageIndex=10]
        [HttpGet]
        [Route("items")]
        [ProducesResponseType(typeof(PaginatedItemsViewModel<CatalogItem>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(IEnumerable<CatalogItem>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Items([FromQuery]int pageSize = 10, [FromQuery]int pageIndex = 0, [FromQuery] string ids = null)
        {
            var cacheFilter = new CacheCatalogFilter { Key = $"{Request.Path.Value}{Request.QueryString}", PageSize = pageSize, PageIndex = pageIndex };


            var pageItemsValue = await _catalogService.ListAsync(ids, null, cacheFilter);

            if (pageItemsValue == null)
            {
                return BadRequest("Ids value invalid. Must be comma-separated list of numbers.");
            }

            var itemsOnPage = ChangeUriPlaceholder(pageItemsValue.ToList());

            var model = new PaginatedItemsViewModel<CatalogItem>(pageIndex, pageSize, (await _catalogService.ListAllAsync()).LongCount(), itemsOnPage);

            return Ok(model);
        }


        [HttpGet]
        [Route("items/{id:int}")]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(CatalogItem), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetItemById(int id)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            var singleItemValue = await _catalogService.GetSingleBySpecAsync(new CatalogFilterSpecification(id), 
                new CacheCatalogFilter() { Key = Request.Path.Value });

            var baseUri = _settings.PicBaseUrl;
            var azureStorageEnabled = _settings.AzureStorageEnabled;
            singleItemValue.FillProductUrl(baseUri, azureStorageEnabled: azureStorageEnabled);

            if (singleItemValue != null)
            {
                return Ok(singleItemValue);
            }

            return NotFound();
        }

        // GET api/v1/[controller]/items/withname/samplename[?pageSize=3&pageIndex=10]
        [HttpGet]
        [Route("[action]/withname/{name:minlength(1)}")]
        [ProducesResponseType(typeof(PaginatedItemsViewModel<CatalogItem>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Items(string name, [FromQuery]int pageSize = 10, [FromQuery]int pageIndex = 0)
        {

            var cacheFilter = new CacheCatalogFilter { Key = $"{Request.Path.Value}{Request.QueryString}", PageSize = pageSize, PageIndex = pageIndex };

            var filteredItemsValue = await _catalogService.ListAsync(new CatalogFilterSpecification(name), cacheFilter);
            var itemsOnPage = ChangeUriPlaceholder(filteredItemsValue.ToList());

            var model = new PaginatedItemsViewModel<CatalogItem>(pageIndex, pageSize, (await _catalogService.ListAllAsync()).LongCount(), itemsOnPage);

            return Ok(model);
        }

        // GET api/v1/[controller]/items/type/1/brand[?pageSize=3&pageIndex=10]
        [HttpGet]
        [Route("[action]/type/{catalogTypeId}/brand/{catalogBrandId:int?}")]
        [ProducesResponseType(typeof(PaginatedItemsViewModel<CatalogItem>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Items(int catalogTypeId, int? catalogBrandId, [FromQuery]int pageSize = 10, [FromQuery]int pageIndex = 0)
        {
            var catalogSpec = new CatalogFilterSpecification(catalogTypeId, catalogBrandId);
            var cacheFilter = new CacheCatalogFilter { Key = $"{Request.Path.Value}{Request.QueryString}", PageSize = pageSize, PageIndex = pageIndex };

            var filteredItemsValue = await _catalogService.ListAsync(catalogSpec, cacheFilter);
            var itemsOnPage = ChangeUriPlaceholder(filteredItemsValue.ToList());

            var model = new PaginatedItemsViewModel<CatalogItem>(
                pageIndex, pageSize, (await _catalogService.ListAllAsync()).LongCount(), itemsOnPage);

            return Ok(model);
        }

        // GET api/v1/[controller]/items/type/all/brand[?pageSize=3&pageIndex=10]
        [HttpGet]
        [Route("[action]/type/all/brand/{catalogBrandId:int?}")]
        [ProducesResponseType(typeof(PaginatedItemsViewModel<CatalogItem>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Items(int? catalogBrandId, [FromQuery]int pageSize = 10, [FromQuery]int pageIndex = 0)
        {
            var catalogSpec = new CatalogFilterSpecification(null, catalogBrandId);
            var cacheFilter = new CacheCatalogFilter { Key = $"{Request.Path.Value}{Request.QueryString}", PageSize = pageSize, PageIndex = pageIndex };

            var filteredItemsValue = await _catalogService.ListAsync(catalogSpec, cacheFilter);
            var itemsOnPage = ChangeUriPlaceholder(filteredItemsValue.ToList());

            var model = new PaginatedItemsViewModel<CatalogItem>(pageIndex, pageSize, (await _catalogService.ListAllAsync()).LongCount(), itemsOnPage);

            return Ok(model);
        }

        // GET api/v1/[controller]/CatalogTypes
        [HttpGet]
        [Route("[action]")]
        [ProducesResponseType(typeof(List<CatalogItem>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> CatalogTypes()
        {
            var totalCatalogTypes = await _catalogService.ListAllCatalogTypeAsync();
            return Ok(totalCatalogTypes);
        }

        // GET api/v1/[controller]/CatalogBrands
        [HttpGet]
        [Route("[action]")]
        [ProducesResponseType(typeof(List<CatalogItem>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> CatalogBrands()
        {
            var totalCatalogBrands = await _catalogService.ListAllCatalogBrandAsync();
            return Ok(totalCatalogBrands);
        }

        //PUT api/v1/[controller]/items
        [Route("items")]
        [HttpPut]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        public async Task<IActionResult> UpdateProduct([FromBody]CatalogItem itemToUpdate)
        {
            List<CatalogItem> totalCatalogItems = (await _catalogService.ListAllAsync()).ToList();

            var catalogItem = totalCatalogItems.SingleOrDefault(i => i.Id == itemToUpdate.Id);

            if (catalogItem == null)
            {
                return NotFound(new { Message = $"Item with id {itemToUpdate.Id} not found." });
            }

            var oldPrice = catalogItem.Price;
            var PriceChanged = oldPrice != itemToUpdate.Price;


            // Update current product
            catalogItem = itemToUpdate;

            if (PriceChanged) // Save product's data and publish integration event through the Event Bus if price has changed
            {
                await _catalogService.UpdateAsync(catalogItem, false);
                //Create Integration Event to be published through the Event Bus
                var priceChangedEvent = new ProductPriceChangedIntegrationEvent(catalogItem.Id, itemToUpdate.Price, oldPrice);

                // Achieving atomicity between original Catalog database operation and the IntegrationEventLog thanks to a local transaction
                await _catalogIntegrationEventService.SaveEventAndCatalogContextChangesAsync(priceChangedEvent);

                // Publish through the Event Bus and mark the saved event as published
                await _catalogIntegrationEventService.PublishThroughEventBusAsync(priceChangedEvent);
            }
            else // Just save the updated product because the Product's Price hasn't changed.
            {
                await _catalogService.UpdateAsync(catalogItem, true);
            }

            return CreatedAtAction(nameof(GetItemById), new { id = itemToUpdate.Id }, null);
        }

        //POST api/v1/[controller]/items
        [Route("items")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        public async Task<IActionResult> CreateProduct([FromBody]CatalogItem product)
        {
            var item = new CatalogItem
            {
                CatalogBrandId = product.CatalogBrandId,
                CatalogTypeId = product.CatalogTypeId,
                Description = product.Description,
                Name = product.Name,
                PictureFileName = product.PictureFileName,
                Price = product.Price
            };

            item = await _catalogService.AddAsync(item, true);            

            return CreatedAtAction(nameof(GetItemById), new { id = item.Id }, null);
        }

        //DELETE api/v1/[controller]/id
        [Route("{id}")]
        [HttpDelete]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            bool? result = await _catalogService.DeleteAsync(id, true);

            if (!result.HasValue)
            {
                return NotFound();
            }

            return NoContent();
        }

        private List<CatalogItem> ChangeUriPlaceholder(List<CatalogItem> items)
        {
            var baseUri = _settings.PicBaseUrl;
            var azureStorageEnabled = _settings.AzureStorageEnabled;

            foreach (var item in items)
            {
                item.FillProductUrl(baseUri, azureStorageEnabled: azureStorageEnabled);
            }

            return items;
        }
    }
}
