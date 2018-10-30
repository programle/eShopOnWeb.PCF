namespace Microsoft.eShopOnContainers.Services.Basket.API.IntegrationEvents.EventHandling
{
    using BuildingBlocks.EventBus.Abstractions;
    using System.Threading.Tasks;
    using Infrastructure;
    using global::Basket.API.Infrastructure;
    using Microsoft.eShopOnContainers.Services.Basket.API.IntegrationEvents.Events;


    public class OrderStatusChangedToPaidIntegrationEventHandler : 
        IIntegrationEventHandler<OrderStatusChangedToPaidIntegrationEvent>
    {
        private readonly BasketContext _BasketContext;

        public OrderStatusChangedToPaidIntegrationEventHandler(BasketContext BasketContext)
        {
            _BasketContext = BasketContext;
        }

        public async Task Handle(OrderStatusChangedToPaidIntegrationEvent command)
        {
            //we're not blocking stock/inventory
            foreach (var orderStockItem in command.OrderStockItems)
            {
                var BasketItem = _BasketContext.BasketItems.Find(orderStockItem.ProductId);

                //BasketItem.RemoveStock(orderStockItem.Units);
            }

            await _BasketContext.SaveChangesAsync();
        }
    }
}