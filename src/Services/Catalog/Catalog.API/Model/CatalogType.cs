using System;

namespace Microsoft.eShopOnContainers.Services.Catalog.API.Model
{
    [Serializable]
    public class CatalogType : BaseEntity
    {
        public string Type { get; set; }
    }
}
