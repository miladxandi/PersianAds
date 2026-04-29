using PersianAds.Models;

namespace PersianAds.Abstractions;

public interface IInterstitialAdService
{
    Task<AdRequestResult> RequestInterstitialAsync(string zoneId, AdRequestOptions? options = null, CancellationToken cancellationToken = default);

    Task ShowInterstitialAsync(string zoneId, string adId, AdShowOptions? options = null, CancellationToken cancellationToken = default);
}
