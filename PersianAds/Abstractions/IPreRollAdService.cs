using PersianAds.Models;

namespace PersianAds.Abstractions;

public interface IPreRollAdService
{
    Task<AdRequestResult> RequestPreRollAsync(string zoneId, PreRollRequestOptions? options = null, CancellationToken cancellationToken = default);

    Task<string> GetPreRollVastUrlAsync(string adId, CancellationToken cancellationToken = default);

    Task ShowPreRollAsync(string zoneId, string adId, PreRollShowOptions? options = null, CancellationToken cancellationToken = default);

    Task DestroyPreRollAsync(string adId, CancellationToken cancellationToken = default);
}
