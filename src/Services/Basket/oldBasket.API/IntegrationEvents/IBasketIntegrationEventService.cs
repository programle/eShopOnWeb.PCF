using Microsoft.eShopOnContainers.BuildingBlocks.EventBus.Events;
using System.Threading.Tasks;

namespace Basket.API.IntegrationEvents
{
    public interface IBasketIntegrationEventService
    {
        Task SaveEventAndBasketContextChangesAsync(IntegrationEvent evt);
        Task PublishThroughEventBusAsync(IntegrationEvent evt);
    }
}
