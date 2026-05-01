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
}
