using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.eShopOnContainers.Services.Catalog.API.Model
{
    [Serializable]
    public abstract class BaseEntity
    {
        public int Id { get; set; }
    }
}
