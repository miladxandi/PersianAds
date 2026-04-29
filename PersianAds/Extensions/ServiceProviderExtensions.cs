using Microsoft.Extensions.DependencyInjection;

namespace PersianAds.Extensions;

public static class ServiceProviderExtensions
{
    public static IServiceCollection AddPersianAdsCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
