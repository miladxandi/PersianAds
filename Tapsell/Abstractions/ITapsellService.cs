using PersianAds.Abstractions;

namespace PersianAds.Tapsell.Abstractions;

public interface ITapsellService :
    IAdNetworkService,
    IRewardedAdService,
    IInterstitialAdService,
    IBannerAdService,
    INativeAdService,
    IPreRollAdService
{
    event EventHandler? InitializationCompleted;

    event EventHandler<global::PersianAds.Tapsell.Models.TapsellRewardedEventArgs>? Rewarded;

    void SetInitializationListener(Action? listener);

    void SetUserConsent(bool userConsent);

    Task<string> RequestBannerNativeAsync(string zoneId, CancellationToken cancellationToken = default);

    Task<string> RequestVideoNativeAsync(string zoneId, CancellationToken cancellationToken = default);
}
