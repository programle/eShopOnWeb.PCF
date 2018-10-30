namespace Microsoft.eShopOnContainers.Services.Basket.API.IntegrationEvents.EventHandling
{
    using BuildingBlocks.EventBus.Abstractions;
    using System.Threading.Tasks;
    using BuildingBlocks.EventBus.Events;
    using Infrastructure;
    using System.Collections.Generic;
    using System.Linq;
    using global::Catalog.API.IntegrationEvents;
    using Microsoft.eShopOnContainers.Services.Basket.API.IntegrationEvents;
    using Microsoft.eShopOnContainers.Services.Catalog.API.IntegrationEvents.Events;

    public class OrderStatusChangedToAwaitingValidationIntegrationEventHandler : 
        IIntegrationEventHandler<OrderStatusChangedToAwaitingValidationIntegrationEvent>
    {
        //private readonly BasketItem _Basket;
        private readonly ICatalogIntegrationEventService _catalogIntegrationEventService;

        //public OrderStatusChangedToAwaitingValidationIntegrationEventHandler(BasketItem Basket,
        //    ICatalogIntegrationEventService catalogIntegrationEventService)
        //{
        //    _Basket = Basket;
        //    _catalogIntegrationEventService = catalogIntegrationEventService;
        //}

        public async Task Handle(OrderStatusChangedToAwaitingValidationIntegrationEvent command)
        {
            //var confirmedOrderStockItems = new List<ConfirmedOrderStockItem>();

            //foreach (var orderStockItem in command.OrderStockItems)
            //{
            //    var catalogItem = _Basket.Find(orderStockItem.ProductId);
            //    var hasStock = catalogItem.AvailableStock >= orderStockItem.Units;
            //    var confirmedOrderStockItem = new ConfirmedOrderStockItem(catalogItem.Id, hasStock);

            //    confirmedOrderStockItems.Add(confirmedOrderStockItem);
            //}

            //var confirmedIntegrationEvent = confirmedOrderStockItems.Any(c => !c.HasStock)
            //    ? (IntegrationEvent)new OrderStockRejectedIntegrationEvent(command.OrderId, confirmedOrderStockItems)
            //    : new OrderStockConfirmedIntegrationEvent(command.OrderId);

            //await _catalogIntegrationEventService.SaveEventAndBasketChangesAsync(confirmedIntegrationEvent);
            //await _catalogIntegrationEventService.PublishThroughEventBusAsync(confirmedIntegrationEvent);
        }
    }
}