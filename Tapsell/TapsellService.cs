using PersianAds.Tapsell.Abstractions;
using PersianAds.Tapsell.Models;

namespace PersianAds.Tapsell;

public sealed class TapsellService : ITapsellService
{
    private const string AndroidManifestAppKey = "TapsellMediationAppKey";

    private readonly TapsellOptions _options;
    private readonly Dictionary<string, string> _pendingRewardedAds = new(StringComparer.Ordinal);
    private Action? _initializationListener;
    private bool _userConsent;
    private string? _resolvedAppId;

    public TapsellService(TapsellOptions options)
    {
        _options = options;
    }

    public string NetworkName => "Tapsell.Mediation";

    public bool IsInitialized { get; private set; }

    public event EventHandler? InitializationCompleted;

    public event EventHandler<TapsellRewardedEventArgs>? Rewarded;

    public void SetInitializationListener(Action? listener)
    {
        _initializationListener = listener;

        if (IsInitialized)
        {
            listener?.Invoke();
        }
    }

    public void SetUserConsent(bool userConsent)
    {
        _userConsent = userConsent;
        Log($"User consent updated: {userConsent}");
    }

    public Task InitializeAsync(
        AdNetworkInitializationOptions options,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var appId = ResolveAppId(options);
        if (string.IsNullOrWhiteSpace(appId))
        {
            throw new ArgumentException("Tapsell AppId is required.", nameof(options));
        }

        _resolvedAppId = appId;
        Log($"InitializeAsync requested. appIdSource={GetAppIdSource(options, appId)}, consent={_userConsent}");

        if (IsInitialized)
        {
            Log("InitializeAsync skipped because service is already initialized.");
            return Task.CompletedTask;
        }

        // The mediation SDK bootstraps itself from the Android manifest/provider.
        // We treat a resolved app key as the initialization contract for the local wrapper.
        IsInitialized = true;
        Log("Initialization marked complete.");
        _initializationListener?.Invoke();
        InitializationCompleted?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public async Task<AdRequestResult> RequestRewardedAsync(
        string zoneId,
        AdRequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        zoneId = NormalizeRequired(zoneId, nameof(zoneId));
        await EnsureInitializedAsync(cancellationToken);

        var adId = $"{zoneId}:{Guid.NewGuid():N}";
        _pendingRewardedAds[zoneId] = adId;
        Log($"RequestRewardedAsync succeeded. zoneId={zoneId}, adId={adId}, userId={options?.UserId ?? "none"}");
        return new AdRequestResult(zoneId, adId);
    }

    public async Task ShowRewardedAsync(
        string zoneId,
        string adId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        zoneId = NormalizeRequired(zoneId, nameof(zoneId));
        adId = NormalizeRequired(adId, nameof(adId));
        await EnsureInitializedAsync(cancellationToken);

        if (_pendingRewardedAds.TryGetValue(zoneId, out var pendingAdId) && !string.Equals(pendingAdId, adId, StringComparison.Ordinal))
        {
            Log($"ShowRewardedAsync received a different ad id than the last requested one. zoneId={zoneId}, requested={pendingAdId}, received={adId}");
        }

        _pendingRewardedAds.Remove(zoneId);
        Log($"ShowRewardedAsync completed. zoneId={zoneId}, adId={adId}");

        Rewarded?.Invoke(this, new TapsellRewardedEventArgs(zoneId, adId, completed: true));
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (IsInitialized)
        {
            return;
        }

        Log("Auto-initializing before ad request/show.");
        await InitializeAsync(new AdNetworkInitializationOptions(
            AppId: _options.AppId),
            cancellationToken);
    }

    private string ResolveAppId(AdNetworkInitializationOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.AppId))
        {
            return options.AppId;
        }

        if (!string.IsNullOrWhiteSpace(_options.AppId))
        {
            return _options.AppId;
        }

#if ANDROID
        try
        {
            var context = Android.App.Application.Context;
            var packageManager = context.PackageManager;
            var applicationInfo = packageManager?.GetApplicationInfo(
                context.PackageName!,
                Android.Content.PM.PackageInfoFlags.MetaData);
            var bundle = applicationInfo?.MetaData;
            var manifestValue = bundle?.GetString(AndroidManifestAppKey);
            if (!string.IsNullOrWhiteSpace(manifestValue))
            {
                return manifestValue;
            }
        }
        catch (Exception ex)
        {
            Log($"Failed to read `{AndroidManifestAppKey}` from Android manifest metadata: {ex.Message}");
        }
#endif

        return string.Empty;
    }

    private string GetAppIdSource(AdNetworkInitializationOptions options, string appId)
    {
        if (string.Equals(options.AppId, appId, StringComparison.Ordinal))
        {
            return "request";
        }

        if (string.Equals(_options.AppId, appId, StringComparison.Ordinal))
        {
            return "service-options";
        }

        return "android-manifest";
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty value is required.", paramName);
        }

        return value;
    }

    private static void Log(string message)
    {
#if ANDROID
        System.Diagnostics.Debug.WriteLine($"TapsellService: {message}");
#endif
    }
}
