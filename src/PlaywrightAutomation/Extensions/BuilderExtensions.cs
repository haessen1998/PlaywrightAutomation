using DecoratR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlaywrightAutomation.Data;
using PlaywrightAutomation.Decorators;
using PlaywrightAutomation.HealthChecks;
using PlaywrightAutomation.Interfaces;
using PlaywrightAutomation.Services;

namespace PlaywrightAutomation.Extensions;

public static class BuilderExtensions
{
    public static TBuilder AddPlaywrightDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        // Add configuration for options
        builder.Services.Configure<RetryOptions>(builder.Configuration.GetSection("Retry"));
        builder.Services.Configure<PlaywrightOptions>(builder.Configuration.GetSection("Playwright"));

        builder.Services.AddSingleton<IPlaywrightContextAccessor, PlaywrightContextAccessor>();

        // Add decoration
        builder.Services.Decorate<IPlaywrightService>()
                .With<PlaywrightLoggingDecorator>()
                .Then<PlaywrightRetryDecorator>()
                .Then<PlaywrightService>()
                .AsSingleton()
                .Apply();

        return builder;

    }

    public static TBuilder AddPlaywrightDefaults<TBuilder>(this TBuilder builder, RetryOptions retry, PlaywrightOptions playwright) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.Configure<RetryOptions>(options =>
        {
            options.MaxRetries = retry.MaxRetries;
            options.RetryIntervalMs = retry.RetryIntervalMs;
        });

        builder.Services.Configure<PlaywrightOptions>(options =>
        {
            options.Headless = playwright.Headless;
            options.Mode = playwright.Mode;
            options.Server = playwright.Server;
            options.Channel = playwright.Channel;
            options.PageIntervalMs = playwright.PageIntervalMs;
            options.ElementIntervalMs = playwright.ElementIntervalMs;
            options.VideoIntervalMs = playwright.VideoIntervalMs;
            options.Slow = playwright.Slow;
            options.Args = playwright.Args;
        });

        builder.Services.AddSingleton<IPlaywrightContextAccessor, PlaywrightContextAccessor>();

        // Add decoration
        builder.Services.Decorate<IPlaywrightService>()
                .With<PlaywrightLoggingDecorator>()
                .Then<PlaywrightRetryDecorator>()
                .Then<PlaywrightService>()
                .AsSingleton()
                .Apply();

        return builder;
    }

    public static TBuilder AddPlaywrightHealthCheck<TBuilder>(this TBuilder builder, string name = "playwright") where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks().AddCheck<PlaywrightHealthCheck>(name);
        return builder;
    }

}
