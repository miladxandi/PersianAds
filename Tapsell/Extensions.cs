using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using PersianAds.Tapsell.Platforms.Android;
using PersianAds.Tapsell.Abstractions;
using PersianAds.Tapsell.Models;
using PersianAds.Tapsell.Views;

namespace PersianAds.Tapsell;

public static class Extensions
{
    public static IServiceCollection AddTapsell(
        this IServiceCollection services,
        Action<TapsellOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new TapsellOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<ITapsellService, TapsellService>();

        return services;
    }

    public static MauiAppBuilder UseTapsell(
        this MauiAppBuilder builder,
        Action<TapsellOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddTapsell(configure);
        builder.ConfigureMauiHandlers(static handlers =>
        {
            handlers.AddHandler(typeof(TapsellBannerHostView), typeof(BannerHostViewHandler));
            handlers.AddHandler(typeof(TapsellNativeHostView), typeof(NativeHostViewHandler));
            handlers.AddHandler(typeof(TapsellPreRollHostView), typeof(PreRollHostViewHandler));
        });

        return builder;
    }
}
