using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.eShopOnContainers.Services.Basket.API.Model;

namespace Microsoft.eShopOnContainers.Services.Basket.API.Model
{
    public static class BasketItemExtensions
    {
        public static void FillProductUrl(this BasketItem item, string picBaseUrl, bool azureStorageEnabled)
        {
            if (item != null)
            {
                item.PictureUrl = azureStorageEnabled
                   ? picBaseUrl + item.PictureUrl
                   : picBaseUrl.Replace("[0]", item.Id.ToString());
            }
        }
    }
}
