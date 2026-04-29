using PersianAds.Models;

namespace PersianAds.Abstractions;

public interface IRewardedAdService
{
    Task<AdRequestResult> RequestRewardedAsync(string zoneId, AdRequestOptions? options = null, CancellationToken cancellationToken = default);

    Task ShowRewardedAsync(string zoneId, string adId, AdShowOptions? options = null, CancellationToken cancellationToken = default);
}
