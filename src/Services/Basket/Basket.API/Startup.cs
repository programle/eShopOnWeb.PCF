using Autofac;
using Microsoft.eShopOnContainers.Services.Basket.API.Model;
using Autofac.Extensions.DependencyInjection;
using global::Basket.API.Infrastructure.Filters;
using global::Basket.API.IntegrationEvents;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.ServiceFabric;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Redis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
//using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.eShopOnContainers.BuildingBlocks.EventBus;
using Microsoft.eShopOnContainers.BuildingBlocks.EventBus.Abstractions;
using Microsoft.eShopOnContainers.BuildingBlocks.EventBusRabbitMQ;
using Microsoft.eShopOnContainers.BuildingBlocks.EventBusServiceBus;
using Microsoft.eShopOnContainers.Services.Basket.API.IntegrationEvents.EventHandling;
using Microsoft.eShopOnContainers.Services.Basket.API.IntegrationEvents.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pivotal.Discovery.Client;
using RabbitMQ.Client;
using System;
using System.Data.Common;
using System.Reflection;
using Steeltoe.CloudFoundry.Connector.SqlServer.EFCore;
using Steeltoe.Management.CloudFoundry;
using Steeltoe.CloudFoundry.Connector.Redis;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.CloudFoundry.Connector.RabbitMQ;
using Basket.API.IntegrationEvents.Events;
using Basket.API.IntegrationEvents.EventHandling;
using StackExchange.Redis;
using Microsoft.eShopOnContainers.Services.Basket.API.Services;

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


            services
                //.AddAppInsight(Configuration)
                .AddCustomMVC(Configuration)
                .AddCustomOptions(Configuration)
                .AddEventBus(Configuration)
                .AddDiscoveryClient(Configuration)
                .AddRabbitMQConnection(Configuration)
                .AddDistributedRedisCache(Configuration)
                .AddRedisConnectionMultiplexer(Configuration)
                .AddSwagger();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddTransient<IBasketRepository, RedisBasketRepository>();
            services.AddTransient<IIdentityService, IdentityService>();


            services.AddSingleton<ConnectionMultiplexer>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<BasketSettings>>().Value;
                //var configuration = ConfigurationOptions.Parse(settings, true);

                //ConfigurationOptions redisconfig = new ConfigurationOptions
                //{
                //    ServiceName = "redis",
                //    EndPoints =
                //        {
                //            { "localhost", 6379 }
                //        },
                //    Password = "testPassword"
                //};

                ////configuration.ResolveDns = true;

                //return ConnectionMultiplexer.Connect(redisconfig);
                return ConnectionMultiplexer.Connect(Configuration.GetValue<string>("redis:client:connectionString"));
            });


            services.AddCloudFoundryActuators(Configuration);

            var container = new ContainerBuilder();
            container.Populate(services);
            return new AutofacServiceProvider(container.Build());

        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            //Configure logs

            loggerFactory.AddAzureWebAppDiagnostics();
            //loggerFactory.AddApplicationInsights(app.ApplicationServices, LogLevel.Trace);

            var pathBase = Configuration["PATH_BASE"];

            if (!string.IsNullOrEmpty(pathBase))
            {
                loggerFactory.CreateLogger("init").LogDebug($"Using PATH BASE '{pathBase}'");
                app.UsePathBase(pathBase);
            }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            app.Map("/liveness", lapp => lapp.Run(async ctx => ctx.Response.StatusCode = 200));
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

            app.UseCors("CorsPolicy");

            app.UseMvcWithDefaultRoute();
            app.UseCloudFoundryActuators();
            app.UseDiscoveryClient();

            app.UseSwagger()
              .UseSwaggerUI(c =>
              {
                  c.SwaggerEndpoint($"{ (!string.IsNullOrEmpty(pathBase) ? pathBase : string.Empty) }/swagger/v1/swagger.json", "Basket.API V1");
              });

            ConfigureEventBus(app);
        }

        protected virtual void ConfigureEventBus(IApplicationBuilder app)
        {
            var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();
            eventBus.Subscribe<ProductPriceChangedIntegrationEvent, ProductPriceChangedIntegrationEventHandler>();
            eventBus.Subscribe<OrderStartedIntegrationEvent, OrderStartedIntegrationEventHandler>();
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
                    Title = "eShopOnContainers - Basket HTTP API",
                    Version = "v1",
                    Description = "The Basket Microservice HTTP API. This is a Data-Driven/CRUD microservice sample",
                    TermsOfService = "Terms Of Service"
                });
            });

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
            services.AddTransient<ProductPriceChangedIntegrationEventHandler>();
            services.AddTransient<OrderStartedIntegrationEventHandler>();

            return services;
        }
    }
}
