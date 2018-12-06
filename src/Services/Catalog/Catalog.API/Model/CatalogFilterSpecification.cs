using Microsoft.eShopOnContainers.Services.Catalog.API.Model;

namespace Catalog.API.Model
{

    public class CatalogFilterSpecification : BaseSpecification<CatalogItem>
    {
        public CatalogFilterSpecification(int? typeId, int? brandId)
            : base(i => (!brandId.HasValue || i.CatalogBrandId == brandId) &&
                (!typeId.HasValue || i.CatalogTypeId == typeId))
        {
        }

        public CatalogFilterSpecification(int? id)
            : base(i => (!id.HasValue || i.Id == id))
        {
        }

        public CatalogFilterSpecification(string name) : base(i => i.Name == name)
        {
        }
    }
}
