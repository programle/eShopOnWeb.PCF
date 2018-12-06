using System;

namespace Microsoft.eShopOnContainers.Services.Catalog.API.Model
{
    [Serializable]
    public class CatalogBrand : BaseEntity
    {
        public string Brand { get; set; }
    }
}
