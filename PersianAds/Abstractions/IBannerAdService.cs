using PersianAds.Models;

namespace PersianAds.Abstractions;

public interface IBannerAdService
{
    Task LoadBannerAsync(string zoneId, BannerAdSize size, CancellationToken cancellationToken = default);

    Task ShowBannerAsync(string zoneId, CancellationToken cancellationToken = default);

    Task HideBannerAsync(string zoneId, CancellationToken cancellationToken = default);
}
