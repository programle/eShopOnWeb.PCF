using Basket.API.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.eShopOnContainers.BuildingBlocks.EventBus.Abstractions;
using Microsoft.eShopOnContainers.BuildingBlocks.EventBus.Events;
using Microsoft.eShopOnContainers.BuildingBlocks.IntegrationEventLogEF.Services;
using Microsoft.eShopOnContainers.BuildingBlocks.IntegrationEventLogEF.Utilities;
using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace Basket.API.IntegrationEvents
{
    public class BasketIntegrationEventService : IBasketIntegrationEventService
    {
        private readonly Func<DbConnection, IIntegrationEventLogService> _integrationEventLogServiceFactory;
        private readonly IEventBus _eventBus;
        private readonly BasketContext _basketContext;
        private readonly IIntegrationEventLogService _eventLogService;

        public BasketIntegrationEventService(IEventBus eventBus, BasketContext basketContext,
        Func<DbConnection, IIntegrationEventLogService> integrationEventLogServiceFactory)
        {
            _basketContext = basketContext ?? throw new ArgumentNullException(nameof(basketContext));
            _integrationEventLogServiceFactory = integrationEventLogServiceFactory ?? throw new ArgumentNullException(nameof(integrationEventLogServiceFactory));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _eventLogService = _integrationEventLogServiceFactory(_basketContext.Database.GetDbConnection());
        }

        public async Task PublishThroughEventBusAsync(IntegrationEvent evt)
        {
            _eventBus.Publish(evt);

            await _eventLogService.MarkEventAsPublishedAsync(evt);
        }

        public async Task SaveEventAndBasketContextChangesAsync(IntegrationEvent evt)
        {
            //Use of an EF Core resiliency strategy when using multiple DbContexts within an explicit BeginTransaction():
            //See: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency            
            await ResilientTransaction.New(_basketContext)
                .ExecuteAsync(async () => {
                    // Achieving atomicity between original catalog database operation and the IntegrationEventLog thanks to a local transaction
                    await _basketContext.SaveChangesAsync();
                    await _eventLogService.SaveEventAsync(evt, _basketContext.Database.CurrentTransaction.GetDbTransaction());
                });
        }
    }
}
