using PersianAds.Tapsell.Models;

namespace PersianAds.Tapsell.Abstractions;

public interface ITapsellService
{
    string NetworkName { get; }

    bool IsInitialized { get; }

    event EventHandler? InitializationCompleted;

    event EventHandler<TapsellRewardedEventArgs>? Rewarded;

    void SetInitializationListener(Action? listener);

    void SetUserConsent(bool userConsent);

    Task InitializeAsync(
        AdNetworkInitializationOptions options,
        CancellationToken cancellationToken = default);

    Task<AdRequestResult> RequestRewardedAsync(
        string zoneId,
        AdRequestOptions? options = null,
        CancellationToken cancellationToken = default);

    Task ShowRewardedAsync(
        string zoneId,
        string adId,
        CancellationToken cancellationToken = default);

    Task<AdRequestResult> RequestInterstitialAsync(
        string zoneId,
        AdRequestOptions? options = null,
        CancellationToken cancellationToken = default);

    Task ShowInterstitialAsync(
        string zoneId,
        string adId,
        CancellationToken cancellationToken = default);

    Task LoadBannerAsync(
        string zoneId,
        BannerAdSize size,
        CancellationToken cancellationToken = default);

    Task ShowBannerAsync(string zoneId, CancellationToken cancellationToken = default);

    Task HideBannerAsync(string zoneId, CancellationToken cancellationToken = default);

    Task<NativeAdResult> RequestNativeAsync(
        string zoneId,
        NativeAdRequestOptions? options = null,
        CancellationToken cancellationToken = default);

    Task ShowNativeAsync(
        string zoneId,
        string adId,
        CancellationToken cancellationToken = default);

    Task<AdRequestResult> RequestPreRollAsync(
        string zoneId,
        PreRollRequestOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<string> GetPreRollVastUrlAsync(string adId, CancellationToken cancellationToken = default);

    Task ShowPreRollAsync(
        string zoneId,
        string adId,
        PreRollShowOptions? options = null,
        CancellationToken cancellationToken = default);

    Task DestroyPreRollAsync(string adId, CancellationToken cancellationToken = default);
}
