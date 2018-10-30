using Autofac;
using Autofac.Extensions.DependencyInjection;
using Basket.API.Infrastructure.Filters;
using Basket.API.Infrastructure.Middlewares;
using Basket.API.IntegrationEvents.EventHandling;
using Basket.API.IntegrationEvents.Events;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.ServiceFabric;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.ServiceBus;
using Microsoft.eShopOnContainers.BuildingBlocks.EventBus;
using Microsoft.eShopOnContainers.BuildingBlocks.EventBus.Abstractions;
using Microsoft.eShopOnContainers.BuildingBlocks.EventBusRabbitMQ;
using Microsoft.eShopOnContainers.BuildingBlocks.EventBusServiceBus;
using Microsoft.eShopOnContainers.Services.Basket.API.IntegrationEvents.EventHandling;
using Microsoft.eShopOnContainers.Services.Basket.API.IntegrationEvents.Events;
using Microsoft.eShopOnContainers.Services.Basket.API.Model;
using Microsoft.eShopOnContainers.Services.Basket.API.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using StackExchange.Redis;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;


using global::Basket.API.IntegrationEvents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.eShopOnContainers.BuildingBlocks.IntegrationEventLogEF;
using Microsoft.eShopOnContainers.BuildingBlocks.IntegrationEventLogEF.Services;
using Microsoft.eShopOnContainers.Services.Basket.API;
using Pivotal.Discovery.Client;
using System.Data.Common;
using System.Reflection;
using Steeltoe.CloudFoundry.Connector.SqlServer.EFCore;
using Steeltoe.Management.CloudFoundry;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.CloudFoundry.Connector.RabbitMQ;
using Basket.API.Infrastructure;

namespace Microsoft.eShopOnContainers.Services.Basket.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }


        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddAppInsight(Configuration)
                .AddCustomMVC(Configuration)
                .AddCustomDbContext(Configuration)
                .AddCustomOptions(Configuration)
                .AddIntegrationServices(Configuration)
                .AddEventBus(Configuration)
                .AddDiscoveryClient(Configuration)
                .AddRabbitMQConnection(Configuration)
                .AddSwagger();

            services.AddCloudFoundryActuators(Configuration);

            var container = new ContainerBuilder();
            container.Populate(services);
            return new AutofacServiceProvider(container.Build());

        }
    }

    public static class CustomExtensionMethods
    {
        public static IServiceCollection AddAppInsight(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddApplicationInsightsTelemetry(configuration);
            var orchestratorType = configuration.GetValue<string>("OrchestratorType");

            if (orchestratorType?.ToUpper() == "K8S")
            {
                // Enable K8s telemetry initializer
                services.EnableKubernetes();
            }
            if (orchestratorType?.ToUpper() == "SF")
            {
                // Enable SF telemetry initializer
                services.AddSingleton<ITelemetryInitializer>((serviceProvider) =>
                    new FabricTelemetryInitializer());
            }

            return services;
        }

        public static IServiceCollection AddCustomMVC(this IServiceCollection services, IConfiguration configuration)
        {
            // services.AddHealthChecks(checks =>
            // {
            //     var minutes = 1;
            //     if (int.TryParse(configuration["HealthCheck:Timeout"], out var minutesParsed))
            //     {
            //         minutes = minutesParsed;
            //     }
            //     checks.AddSqlCheck("CatalogDb", configuration["ConnectionString"], TimeSpan.FromMinutes(minutes));

            //     var accountName = configuration.GetValue<string>("AzureStorageAccountName");
            //     var accountKey = configuration.GetValue<string>("AzureStorageAccountKey");
            //     if (!string.IsNullOrEmpty(accountName) && !string.IsNullOrEmpty(accountKey))
            //     {
            //         checks.AddAzureBlobStorageCheck(accountName, accountKey);
            //     }
            // });

            services.AddMvc(options =>
            {
                options.Filters.Add(typeof(HttpGlobalExceptionFilter));
            }).AddControllersAsServices();

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            });

            return services;
        }

        public static IServiceCollection AddCustomDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<BasketContext>(options =>
            {
                options.UseSqlServer(configuration);

                    // Changing default behavior when client evaluation occurs to throw. 
                    // Default in EF Core would be to log a warning when client evaluation is performed.
                    options.ConfigureWarnings(warnings => warnings.Throw(RelationalEventId.QueryClientEvaluationWarning));
                    //Check Client vs. Server evaluation: https://docs.microsoft.com/en-us/ef/core/querying/client-eval
                });

            services.AddDbContext<IntegrationEventLogContext>(options =>
            {
                options.UseSqlServer(configuration);
            });

            return services;
        }

        public static IServiceCollection AddCustomOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<BasketSettings>(configuration);
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var problemDetails = new ValidationProblemDetails(context.ModelState)
                    {
                        Instance = context.HttpContext.Request.Path,
                        Status = StatusCodes.Status400BadRequest,
                        Detail = "Please refer to the errors property for additional details."
                    };

                    return new BadRequestObjectResult(problemDetails)
                    {
                        ContentTypes = { "application/problem+json", "application/problem+xml" }
                    };
                };
            });

            return services;
        }

        public static IServiceCollection AddSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.DescribeAllEnumsAsStrings();
                options.SwaggerDoc("v1", new Swashbuckle.AspNetCore.Swagger.Info
                {
                    Title = "eShopOnContainers - Catalog HTTP API",
                    Version = "v1",
                    Description = "The Catalog Microservice HTTP API. This is a Data-Driven/CRUD microservice sample",
                    TermsOfService = "Terms Of Service"
                });
            });

            return services;

        }

        public static IServiceCollection AddIntegrationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<Func<DbConnection, IIntegrationEventLogService>>(
               sp => (DbConnection c) => new IntegrationEventLogService(c));

            //services.AddTransient<ICatalogIntegrationEventService, CatalogIntegrationEventService>();

            if (configuration.GetValue<bool>("AzureServiceBusEnabled"))
            {
                services.AddSingleton<IServiceBusPersisterConnection>(sp =>
                {
                    var settings = sp.GetRequiredService<IOptions<BasketSettings>>().Value;
                    var logger = sp.GetRequiredService<ILogger<DefaultServiceBusPersisterConnection>>();

                    var serviceBusConnection = new ServiceBusConnectionStringBuilder(settings.EventBusConnection);

                    return new DefaultServiceBusPersisterConnection(serviceBusConnection, logger);
                });
            }
            else
            {
                // services.AddSingleton<IRabbitMQPersistentConnection>(sp =>
                // {
                //     var settings = sp.GetRequiredService<IOptions<CatalogSettings>>().Value;
                //     var logger = sp.GetRequiredService<ILogger<DefaultRabbitMQPersistentConnection>>();

                //     var factory = new ConnectionFactory()
                //     {
                //         HostName = configuration["EventBusConnection"],
                //         //VirtualHost = configuration["EventBusVirtualHost"],
                //         RequestedHeartbeat = 60     
                //     };

                //     if (!string.IsNullOrEmpty(configuration["EventBusVirtualHost"]))
                //     {
                //         factory.VirtualHost = configuration["EventBusVirtualHost"];
                //     }

                //     if (!string.IsNullOrEmpty(configuration["EventBusUserName"]))
                //     {
                //         factory.UserName = configuration["EventBusUserName"];
                //     }

                //     if (!string.IsNullOrEmpty(configuration["EventBusPassword"]))
                //     {
                //         factory.Password = configuration["EventBusPassword"];
                //     }

                //     var retryCount = 5;
                //     if (!string.IsNullOrEmpty(configuration["EventBusRetryCount"]))
                //     {
                //         retryCount = int.Parse(configuration["EventBusRetryCount"]);
                //     }

                //     return new DefaultRabbitMQPersistentConnection(factory, logger, retryCount);
                // });
            }

            return services;
        }

        public static IServiceCollection AddEventBus(this IServiceCollection services, IConfiguration configuration)
        {
            var subscriptionClientName = configuration["SubscriptionClientName"];

            if (configuration.GetValue<bool>("AzureServiceBusEnabled"))
            {
                services.AddSingleton<IEventBus, EventBusServiceBus>(sp =>
                {
                    var serviceBusPersisterConnection = sp.GetRequiredService<IServiceBusPersisterConnection>();
                    var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                    var logger = sp.GetRequiredService<ILogger<EventBusServiceBus>>();
                    var eventBusSubcriptionsManager = sp.GetRequiredService<IEventBusSubscriptionsManager>();

                    return new EventBusServiceBus(serviceBusPersisterConnection, logger,
                        eventBusSubcriptionsManager, subscriptionClientName, iLifetimeScope);
                });

            }
            else
            {
                services.AddSingleton<IEventBus, EventBusRabbitMQ>(sp =>
                {
                    var connectionFactory = sp.GetRequiredService<ConnectionFactory>();
                    var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
                    var logger = sp.GetRequiredService<ILogger<EventBusRabbitMQ>>();
                    var eventBusSubcriptionsManager = sp.GetRequiredService<IEventBusSubscriptionsManager>();

                    var retryCount = 5;
                        // if (!string.IsNullOrEmpty(configuration["EventBusRetryCount"]))
                        // {
                        //     retryCount = int.Parse(configuration["EventBusRetryCount"]);
                        // }
                        var rmqlogger = sp.GetRequiredService<ILogger<DefaultRabbitMQPersistentConnection>>();
                    var rabbitMQPersistentConnection = new DefaultRabbitMQPersistentConnection(connectionFactory, rmqlogger, retryCount);
                    return new EventBusRabbitMQ(rabbitMQPersistentConnection, logger, iLifetimeScope, eventBusSubcriptionsManager, subscriptionClientName, retryCount);
                });
            }

            services.AddSingleton<IEventBusSubscriptionsManager, InMemoryEventBusSubscriptionsManager>();
            //services.AddTransient<OrderStatusChangedToAwaitingValidationIntegrationEventHandler>();
            //services.AddTransient<OrderStatusChangedToPaidIntegrationEventHandler>();

            return services;
        }
    }
}
