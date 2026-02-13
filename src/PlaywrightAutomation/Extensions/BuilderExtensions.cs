using DecoratR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlaywrightAutomation.Data;
using PlaywrightAutomation.Decorators;
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

        // Add decoration
        builder.Services.Decorate<IPlaywrightService>()
                .With<PlaywrightLoggingDecorator>()
                .Then<PlaywrightRetryDecorator>()
                .Then<PlaywrightService>()
                .AsSingleton()
                .Apply();

        return builder;

    }

    public static TBuilder AddPlaywrightDefaults<TBuilder>(this TBuilder builder,RetryOptions retry, PlaywrightOptions playwright) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.Configure<RetryOptions>(options => options = retry);
        builder.Services.Configure<PlaywrightOptions>(options => options = playwright);

        // Add decoration
        builder.Services.Decorate<IPlaywrightService>()
                .With<PlaywrightLoggingDecorator>()
                .Then<PlaywrightRetryDecorator>()
                .Then<PlaywrightService>()
                .AsSingleton()
                .Apply();

        return builder;
    }

}
