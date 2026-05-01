using Microsoft.Extensions.DependencyInjection;
using PersianAds.Tapsell.Abstractions;
using PersianAds.Tapsell.Models;

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
}
