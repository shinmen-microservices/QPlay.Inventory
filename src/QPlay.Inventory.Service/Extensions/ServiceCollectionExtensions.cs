using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using QPlay.Common.MongoDB;
using QPlay.Inventory.Service.Clients;
using QPlay.Inventory.Service.Models.Entities;
using System;
using System.Net.Http;

namespace QPlay.Inventory.Service.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection ConfigureHttpClient(this IServiceCollection services)
    {
        Random jitter = new();

        services.AddHttpClient<CatalogClient>(client => client.BaseAddress = new Uri("https://localhost:5001"))
            .AddPolicyHandler((serviceProvider, request) => Policy.WrapAsync(
                HttpPolicyExtensions.HandleTransientHttpError().Or<TimeoutRejectedException>().WaitAndRetryAsync(
                    5,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + TimeSpan.FromMilliseconds(jitter.Next(0, 1000)),
                    onRetry: (outcome, timeSpan, retryAttempt, context) => serviceProvider.GetService<ILogger<CatalogClient>>()?
                        .LogWarning($"Delaying for {timeSpan.TotalSeconds} seconds, then making retry {retryAttempt}")
                ),
                HttpPolicyExtensions.HandleTransientHttpError().Or<TimeoutRejectedException>().CircuitBreakerAsync(
                    3,
                    TimeSpan.FromSeconds(15),
                    onBreak: (outcome, timeSpan) => serviceProvider.GetService<ILogger<CatalogClient>>()?
                        .LogWarning($"Opening the circuit for {timeSpan.TotalSeconds} seconds."),
                    onReset: () => serviceProvider.GetService<ILogger<CatalogClient>>()?.LogWarning($"Closing the circuit.")
                ),
                Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(1))
            ));

        return services;
    }

    public static IServiceCollection ConfigureMongo(this IServiceCollection services)
    {
        services.AddMongo()
            .AddMongoRepository<InventoryItem>("inventoryitems")
            .AddMongoRepository<CatalogItem>("catalogitems");
        return services;
    }

    public static IServiceCollection ConfigureControllers(this IServiceCollection services)
    {
        services.AddControllers(options => options.SuppressAsyncSuffixInActionNames = false);
        return services;
    }

    /**
    private static void AddCatalogClient(WebApplicationBuilder builder)
    {
        Random jitter = new();
        ILogger logger = GetLogger();

        builder.Services
            .AddHttpClient<CatalogClient>(client => client.BaseAddress = new Uri("https://localhost:5001"))
            .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.Or<TimeoutRejectedException>().WaitAndRetryAsync(
                5,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + TimeSpan.FromMilliseconds(jitter.Next(0, 1000)),
                onRetry: (outcome, timeSpan, retryAttempt, context) => logger?.LogWarning($"Delaying for {timeSpan.TotalSeconds} seconds, then making retry {retryAttempt}")
            ))
            .AddTransientHttpErrorPolicy(builder => builder.Or<TimeoutRejectedException>().CircuitBreakerAsync(
                3,
                TimeSpan.FromSeconds(15),
                onBreak: (outcome, timeSpan) => logger?.LogWarning($"Opening the circuit for {timeSpan.TotalSeconds} seconds."),
                onReset: () => logger?.LogWarning($"Closing the circuit.")
            ))
            .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(1)));
    }

    private static ILogger GetLogger()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddConsole();
        });
        var logger = loggerFactory.CreateLogger("Program");
        return logger;
    }
    */
}
