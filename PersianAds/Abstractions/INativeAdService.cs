using PersianAds.Models;

namespace PersianAds.Abstractions;

public interface INativeAdService
{
    Task<NativeAdRequestResult> RequestNativeAsync(string zoneId, NativeAdRequestOptions? options = null, CancellationToken cancellationToken = default);

    Task ShowNativeAsync(string zoneId, string adId, CancellationToken cancellationToken = default);
}
