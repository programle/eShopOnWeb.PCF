﻿namespace Microsoft.eShopOnContainers.Services.Basket.API.IntegrationEvents.Events
{
    using BuildingBlocks.EventBus.Events;

    public class OrderStockConfirmedIntegrationEvent : IntegrationEvent
    {
        public int OrderId { get; }

        public OrderStockConfirmedIntegrationEvent(int orderId) => OrderId = orderId;
    }
}