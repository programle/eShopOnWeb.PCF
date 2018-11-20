﻿using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using Ardalis.GuardClauses;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.Comm;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.eShopWeb.Web.Services
{
    public class OrderService : IOrderService
    {
        private readonly ILogger<OrderService> _logger;
        private readonly string _remoteServiceBaseUrl;
        private IAsyncRepository<Basket> _basketRepository;
        private IAsyncRepository<CatalogItem> _catalogItemRepository;
        private HttpClient _httpClient;

        public OrderService(IAsyncRepository<CatalogItem> catalogItemRepository , IAsyncRepository<Basket> basketRepository, HttpClient httpClient, ILogger<OrderService> logger )
        {
            _catalogItemRepository = catalogItemRepository;
            _httpClient = httpClient;
            _basketRepository = basketRepository;
            _remoteServiceBaseUrl = $"{httpClient.BaseAddress}api/v1/orders/";
            _logger = logger;
        }

        public async Task<bool> CreateOrderAsync(int basketId, Address shippingAddress, string UserId)
        {
            var basket = await _basketRepository.GetByIdAsync(basketId);
            Guard.Against.NullBasket(basketId, basket);
            var items = new List<OrderItem>();
            foreach (var item in basket.Items)
            {

                var responseString = await _httpClient.GetStringAsync(uri);

                var catalogItem = await _catalogItemRepository.GetByIdAsync(item.CatalogItemId);
                var itemOrdered = new CatalogItemOrdered(catalogItem.Id, catalogItem.Name, catalogItem.PictureUri);
                var orderItem = new OrderItem(itemOrdered, item.UnitPrice, item.Quantity);
                items.Add(orderItem);
            }
            var order = new Order(basket.BuyerId, shippingAddress, items); // old mono order
       //var Ordering = new Ordering();

            await _orderRepository.AddAsync(order);
        }
    }
}
