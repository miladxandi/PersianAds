using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using Microsoft.Maui.ApplicationModel;
using PersianAds.Models;
using PersianAds.Tapsell.Abstractions;
using PersianAds.Tapsell.Models;

namespace PersianAds.Tapsell;

public sealed class TapsellService : ITapsellService
{
    private readonly TapsellOptions _options;
    private readonly object _sync = new();
    private readonly Dictionary<string, BannerState> _bannerStates = new(StringComparer.Ordinal);
    private readonly Dictionary<nint, HostedBannerState> _hostedBanners = new();
    private readonly Dictionary<string, NativeBannerAdState> _nativeBannerAds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, NativeVideoAdState> _nativeVideoAds = new(StringComparer.Ordinal);
    private readonly Dictionary<nint, HostedNativeState> _hostedNativeAds = new();
    private readonly Dictionary<string, PreRollState> _preRollStates = new(StringComparer.Ordinal);
    private readonly Dictionary<nint, HostedPreRollState> _hostedPreRolls = new();
    private Action? _initializationListener;
    private bool? _userConsent;

    public TapsellService(TapsellOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string NetworkName => "Tapsell";

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

        if (OperatingSystem.IsAndroid())
        {
            global::IR.Tapsell.Sdk.Tapsell.SetGDPRConsent(userConsent);
        }
    }

    public async Task<string> RequestBannerNativeAsync(string zoneId, CancellationToken cancellationToken = default)
    {
        var result = await RequestNativeAsync(
            zoneId,
            new NativeAdRequestOptions(Format: NativeAdFormat.Banner, Count: 1),
            cancellationToken).ConfigureAwait(false);

        return result.AdIds[0];
    }

    public async Task<string> RequestVideoNativeAsync(string zoneId, CancellationToken cancellationToken = default)
    {
        var result = await RequestNativeAsync(
            zoneId,
            new NativeAdRequestOptions(Format: NativeAdFormat.Video, Count: 1),
            cancellationToken).ConfigureAwait(false);

        return result.AdIds[0];
    }

    public async Task InitializeAsync(
        AdNetworkInitializationOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.AppId))
        {
            throw new ArgumentException("Tapsell AppId is required.", nameof(options));
        }

        if (IsInitialized)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            lock (_sync)
            {
                if (IsInitialized)
                {
                    return;
                }

                var application = GetApplication();
                global::IR.Tapsell.Sdk.Tapsell.Initialize(application, options.AppId);
                global::IR.Tapsell.Sdk.Tapsell.SetDebugMode(application, _options.EnableDebugMode);

                if (_userConsent.HasValue)
                {
                    global::IR.Tapsell.Sdk.Tapsell.SetGDPRConsent(_userConsent.Value);
                }

                IsInitialized = true;
            }
        });

        _initializationListener?.Invoke();
        InitializationCompleted?.Invoke(this, EventArgs.Empty);
    }

    public Task<AdRequestResult> RequestRewardedAsync(
        string zoneId,
        AdRequestOptions? options = null,
        CancellationToken cancellationToken = default)
        => RequestFullscreenAdAsync(zoneId, cancellationToken);

    public Task ShowRewardedAsync(
        string zoneId,
        string adId,
        AdShowOptions? options = null,
        CancellationToken cancellationToken = default)
        => ShowFullscreenAdAsync(zoneId, adId, options, raiseRewardedEvent: true, cancellationToken);

    public Task<AdRequestResult> RequestInterstitialAsync(
        string zoneId,
        AdRequestOptions? options = null,
        CancellationToken cancellationToken = default)
        => RequestFullscreenAdAsync(zoneId, cancellationToken);

    public Task ShowInterstitialAsync(
        string zoneId,
        string adId,
        AdShowOptions? options = null,
        CancellationToken cancellationToken = default)
        => ShowFullscreenAdAsync(zoneId, adId, options, raiseRewardedEvent: false, cancellationToken);

    public async Task LoadBannerAsync(string zoneId, BannerAdSize size, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        cancellationToken.ThrowIfCancellationRequested();
        zoneId = NormalizeRequired(zoneId, nameof(zoneId));

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var root = GetRootContent();
            var context = GetContext();

            lock (_sync)
            {
                if (_bannerStates.TryGetValue(zoneId, out var existing))
                {
                    existing.View.Destroy();
                    existing.Container.RemoveAllViews();
                    root.RemoveView(existing.Container);
                    _bannerStates.Remove(zoneId);
                }

                var container = CreateOverlayContainer(GravityFlags.Bottom | GravityFlags.CenterHorizontal, matchParentWidth: true);
                var listener = new BannerListener(completion);
                var view = new global::IR.Tapsell.Sdk.Bannerads.TapsellBannerView(
                    context,
                    MapBannerType(size),
                    zoneId,
                    listener);

                container.Visibility = ViewStates.Gone;
                container.AddView(view, new FrameLayout.LayoutParams(
                    ViewGroup.LayoutParams.WrapContent,
                    ViewGroup.LayoutParams.WrapContent,
                    GravityFlags.Bottom | GravityFlags.CenterHorizontal));
                root.AddView(container);

                _bannerStates[zoneId] = new BannerState(zoneId, container, view, listener);
                view.LoadAd(context, zoneId, MapBannerType(size));
            }
        });

        await completion.Task.ConfigureAwait(false);
    }

    public async Task ShowBannerAsync(string zoneId, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        cancellationToken.ThrowIfCancellationRequested();
        zoneId = NormalizeRequired(zoneId, nameof(zoneId));

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            BannerState state;
            lock (_sync)
            {
                if (!_bannerStates.TryGetValue(zoneId, out state!))
                {
                    throw new InvalidOperationException($"Banner for zone '{zoneId}' is not loaded.");
                }
            }

            state.Container.Visibility = ViewStates.Visible;
            state.View.ShowBannerView();
        });
    }

    public async Task HideBannerAsync(string zoneId, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        cancellationToken.ThrowIfCancellationRequested();
        zoneId = NormalizeRequired(zoneId, nameof(zoneId));

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            BannerState? state;
            lock (_sync)
            {
                _bannerStates.TryGetValue(zoneId, out state);
            }

            if (state is null)
            {
                return;
            }

            state.View.HideBannerView();
            state.Container.Visibility = ViewStates.Gone;
        });
    }

    public async Task<NativeAdRequestResult> RequestNativeAsync(
        string zoneId,
        NativeAdRequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        cancellationToken.ThrowIfCancellationRequested();
        zoneId = NormalizeRequired(zoneId, nameof(zoneId));
        options ??= new NativeAdRequestOptions();

        return options.Format == NativeAdFormat.Video
            ? await RequestNativeVideoAsync(zoneId, options, cancellationToken).ConfigureAwait(false)
            : await RequestNativeBannerAsync(zoneId, options, cancellationToken).ConfigureAwait(false);
    }

    public async Task ShowNativeAsync(string zoneId, string adId, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        cancellationToken.ThrowIfCancellationRequested();
        zoneId = NormalizeRequired(zoneId, nameof(zoneId));
        adId = NormalizeRequired(adId, nameof(adId));

        NativeBannerAdState? bannerState;
        NativeVideoAdState? videoState;

        lock (_sync)
        {
            _nativeBannerAds.TryGetValue(adId, out bannerState);
            _nativeVideoAds.TryGetValue(adId, out videoState);
        }

        if (bannerState is not null)
        {
            await ShowNativeBannerAsync(zoneId, bannerState, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (videoState is not null)
        {
            await ShowNativeVideoAsync(zoneId, videoState, cancellationToken).ConfigureAwait(false);
            return;
        }

        throw new InvalidOperationException($"Native ad '{adId}' is not available.");
    }

    public Task<AdRequestResult> RequestPreRollAsync(
        string zoneId,
        PreRollRequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        cancellationToken.ThrowIfCancellationRequested();
        zoneId = NormalizeRequired(zoneId, nameof(zoneId));

        var adId = Guid.NewGuid().ToString("N");

        lock (_sync)
        {
            _preRollStates[adId] = new PreRollState(adId, zoneId, options);
        }

        return Task.FromResult(new AdRequestResult(adId, zoneId, DateTimeOffset.UtcNow));
    }

    public async Task<string> GetPreRollVastUrlAsync(
        string adId,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        cancellationToken.ThrowIfCancellationRequested();
        adId = NormalizeRequired(adId, nameof(adId));

        string zoneId;
        lock (_sync)
        {
            if (!_preRollStates.TryGetValue(adId, out var state))
            {
                throw new InvalidOperationException($"PreRoll ad '{adId}' is not available.");
            }

            zoneId = state.ZoneId;
        }

        return await MainThread.InvokeOnMainThreadAsync(() =>
            global::IR.Tapsell.Sdk.Tapsell.GetVastTag(zoneId)
            ?? throw new InvalidOperationException("Tapsell returned an empty VAST url.")).ConfigureAwait(false);
    }

    public async Task ShowPreRollAsync(
        string zoneId,
        string adId,
        PreRollShowOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        cancellationToken.ThrowIfCancellationRequested();
        zoneId = NormalizeRequired(zoneId, nameof(zoneId));
        adId = NormalizeRequired(adId, nameof(adId));

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var root = GetRootContent();
            var context = GetContext();
            var activity = GetActivity();

            PreRollState state;
            lock (_sync)
            {
                if (!_preRollStates.TryGetValue(adId, out state!))
                {
                    throw new InvalidOperationException($"PreRoll ad '{adId}' is not available.");
                }

                if (state.Ad is not null)
                {
                    state.Ad.DestroyAd();
                    if (state.Container?.Parent is ViewGroup oldParent)
                    {
                        oldParent.RemoveView(state.Container);
                    }
                }
            }

            var container = CreateOverlayContainer(GravityFlags.Center, matchParentWidth: true, matchParentHeight: true);
            var adContainer = new FrameLayout(context);
            adContainer.SetBackgroundColor(Android.Graphics.Color.Black);
            var videoView = new VideoView(context);
            adContainer.AddView(videoView, new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent,
                GravityFlags.Center));
            container.AddView(adContainer, new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent,
                GravityFlags.Center));

            FrameLayout? companionContainer = null;
            if (options?.HasCompanionBanner == true)
            {
                companionContainer = new FrameLayout(context);
                var companionParams = new FrameLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.WrapContent,
                    GravityFlags.Bottom);
                container.AddView(companionContainer, companionParams);
            }

            root.AddView(container);

            var builder = new global::IR.Tapsell.Sdk.Preroll.TapsellPrerollAd.Builder(activity);
            builder = builder.SetAdContainer(adContainer)
                ?? throw new InvalidOperationException("Failed to configure PreRoll ad container.");
            builder = builder.SetVideoPlayer(videoView)
                ?? throw new InvalidOperationException("Failed to configure PreRoll video view.");
            builder = builder.SetShowTapsellContainer(global::Java.Lang.Boolean.ValueOf(true))
                ?? throw new InvalidOperationException("Failed to configure PreRoll container visibility.");

            if (companionContainer is not null)
            {
                builder = builder.SetCompanionContainer(companionContainer)
                    ?? throw new InvalidOperationException("Failed to configure PreRoll companion container.");
            }

            if (!string.IsNullOrWhiteSpace(options?.ContentUrl))
            {
                builder = builder.SetVideoPath(options.ContentUrl)
                    ?? throw new InvalidOperationException("Failed to configure PreRoll content url.");
            }

            var ad = builder.Build()
                ?? throw new InvalidOperationException("Failed to create PreRoll ad.");

            lock (_sync)
            {
                _preRollStates[adId] = state with
                {
                    Container = container,
                    AdContainer = adContainer,
                    VideoView = videoView,
                    CompanionContainer = companionContainer,
                    Ad = ad
                };
            }

            ad.RequestAd(zoneId);
        });
    }

    public async Task DestroyPreRollAsync(string adId, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        cancellationToken.ThrowIfCancellationRequested();
        adId = NormalizeRequired(adId, nameof(adId));

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            PreRollState? state;
            lock (_sync)
            {
                _preRollStates.TryGetValue(adId, out state);
                _preRollStates.Remove(adId);
            }

            if (state?.Ad is not null)
            {
                state.Ad.DestroyAd();
            }

            if (state?.Container?.Parent is ViewGroup parent)
            {
                parent.RemoveView(state.Container);
            }
        });
    }

    internal async Task AttachBannerHostAsync(
        string zoneId,
        BannerAdSize size,
        FrameLayout container,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        cancellationToken.ThrowIfCancellationRequested();
        zoneId = NormalizeRequired(zoneId, nameof(zoneId));

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var key = container.Handle;
            container.RemoveAllViews();

            lock (_sync)
            {
                if (_hostedBanners.TryGetValue(key, out var existing))
                {
                    existing.View.Destroy();
                    _hostedBanners.Remove(key);
                }
            }

            var context = GetContext();
            var listener = new BannerListener(completion);
            var view = new global::IR.Tapsell.Sdk.Bannerads.TapsellBannerView(
                context,
                MapBannerType(size),
                zoneId,
                listener);

            container.AddView(view, new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.WrapContent,
                GravityFlags.Center));

            lock (_sync)
            {
                _hostedBanners[key] = new HostedBannerState(container, view, listener);
            }

            view.LoadAd(context, zoneId, MapBannerType(size));
            view.ShowBannerView();
        });

        await completion.Task.ConfigureAwait(false);
    }

    internal Task DetachBannerHostAsync(FrameLayout container)
    {
        if (container.Handle == IntPtr.Zero)
        {
            return Task.CompletedTask;
        }

        return MainThread.InvokeOnMainThreadAsync(() =>
        {
            lock (_sync)
            {
                if (_hostedBanners.TryGetValue(container.Handle, out var state))
                {
                    state.View.Destroy();
                    state.Container.RemoveAllViews();
                    _hostedBanners.Remove(container.Handle);
                }
            }
        });
    }

    internal async Task AttachNativeHostAsync(
        string zoneId,
        string adId,
        NativeAdFormat format,
        FrameLayout container,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        cancellationToken.ThrowIfCancellationRequested();
        zoneId = NormalizeRequired(zoneId, nameof(zoneId));
        adId = NormalizeRequired(adId, nameof(adId));

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var key = container.Handle;
            container.RemoveAllViews();

            lock (_sync)
            {
                _hostedNativeAds.Remove(key);
            }

            if (format == NativeAdFormat.Banner)
            {
                if (!_nativeBannerAds.TryGetValue(adId, out var bannerState))
                {
                    throw new InvalidOperationException($"Native banner ad '{adId}' is not available.");
                }

                var context = GetContext();
                var showListener = new NativeBannerShowListener();
                var builder = new global::IR.Tapsell.Sdk.Nativeads.TapsellNativeBannerManager.Builder();
                builder = builder.SetParentView(container)
                    ?? throw new InvalidOperationException("Failed to configure native banner parent view.");
                builder = builder.WithShowListener(showListener)
                    ?? throw new InvalidOperationException("Failed to configure native banner show listener.");

                var viewManager = builder.InflateTemplate(context)
                    ?? throw new InvalidOperationException("Failed to create native banner template.");
                viewManager.NativeAdShowListener = showListener;

                global::IR.Tapsell.Sdk.Nativeads.TapsellNativeBannerManager.BindAd(
                    context,
                    viewManager,
                    zoneId,
                    bannerState.AdId);

                bannerState.Container = container;
                _hostedNativeAds[key] = new HostedNativeState(container);
                return;
            }

            if (!_nativeVideoAds.TryGetValue(adId, out var videoState))
            {
                throw new InvalidOperationException($"Native video ad '{adId}' is not available.");
            }

            var listener = new NativeVideoShowListener();
            videoState.Ad.SetShowListener(listener);
            videoState.Ad.AddToParentView(container);
            videoState.Container = container;
            _hostedNativeAds[key] = new HostedNativeState(container, videoState.Ad);
        });
    }

    internal Task DetachNativeHostAsync(FrameLayout container)
    {
        if (container.Handle == IntPtr.Zero)
        {
            return Task.CompletedTask;
        }

        return MainThread.InvokeOnMainThreadAsync(() =>
        {
            lock (_sync)
            {
                if (_hostedNativeAds.TryGetValue(container.Handle, out var state))
                {
                    state.VideoAd?.RemoveFromParentView(container);
                    state.Container.RemoveAllViews();
                    _hostedNativeAds.Remove(container.Handle);
                }
            }
        });
    }

    internal async Task AttachPreRollHostAsync(
        string zoneId,
        string adId,
        PreRollShowOptions options,
        FrameLayout container,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        cancellationToken.ThrowIfCancellationRequested();
        zoneId = NormalizeRequired(zoneId, nameof(zoneId));
        adId = NormalizeRequired(adId, nameof(adId));

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (!_preRollStates.TryGetValue(adId, out var state))
            {
                throw new InvalidOperationException($"PreRoll ad '{adId}' is not available.");
            }

            if (_hostedPreRolls.TryGetValue(container.Handle, out var existing))
            {
                existing.Ad?.DestroyAd();
                existing.Container.RemoveAllViews();
                _hostedPreRolls.Remove(container.Handle);
            }

            container.RemoveAllViews();

            var context = GetContext();
            var activity = GetActivity();
            var adContainer = new FrameLayout(context);
            adContainer.SetBackgroundColor(Android.Graphics.Color.Black);
            var videoView = new VideoView(context);
            adContainer.AddView(videoView, new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent,
                GravityFlags.Center));
            container.AddView(adContainer, new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent,
                GravityFlags.Center));

            FrameLayout? companionContainer = null;
            if (options.HasCompanionBanner)
            {
                companionContainer = new FrameLayout(context);
                container.AddView(companionContainer, new FrameLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.WrapContent,
                    GravityFlags.Bottom));
            }

            var builder = new global::IR.Tapsell.Sdk.Preroll.TapsellPrerollAd.Builder(activity);
            builder = builder.SetAdContainer(adContainer)
                ?? throw new InvalidOperationException("Failed to configure PreRoll ad container.");
            builder = builder.SetVideoPlayer(videoView)
                ?? throw new InvalidOperationException("Failed to configure PreRoll video view.");
            builder = builder.SetShowTapsellContainer(global::Java.Lang.Boolean.ValueOf(true))
                ?? throw new InvalidOperationException("Failed to configure PreRoll container visibility.");

            if (companionContainer is not null)
            {
                builder = builder.SetCompanionContainer(companionContainer)
                    ?? throw new InvalidOperationException("Failed to configure PreRoll companion container.");
            }

            if (!string.IsNullOrWhiteSpace(options.ContentUrl))
            {
                builder = builder.SetVideoPath(options.ContentUrl)
                    ?? throw new InvalidOperationException("Failed to configure PreRoll content url.");
            }

            var ad = builder.Build()
                ?? throw new InvalidOperationException("Failed to create PreRoll ad.");

            _preRollStates[adId] = state with
            {
                Container = container,
                AdContainer = adContainer,
                VideoView = videoView,
                CompanionContainer = companionContainer,
                Ad = ad
            };

            _hostedPreRolls[container.Handle] = new HostedPreRollState(container, ad);
            ad.RequestAd(zoneId);
        });
    }

    internal Task DetachPreRollHostAsync(FrameLayout container)
    {
        if (container.Handle == IntPtr.Zero)
        {
            return Task.CompletedTask;
        }

        return MainThread.InvokeOnMainThreadAsync(() =>
        {
            lock (_sync)
            {
                if (_hostedPreRolls.TryGetValue(container.Handle, out var state))
                {
                    state.Ad?.DestroyAd();
                    state.Container.RemoveAllViews();
                    _hostedPreRolls.Remove(container.Handle);
                }
            }
        });
    }

    private async Task<AdRequestResult> RequestFullscreenAdAsync(
        string zoneId,
        CancellationToken cancellationToken)
    {
        EnsureInitialized();
        cancellationToken.ThrowIfCancellationRequested();
        zoneId = NormalizeRequired(zoneId, nameof(zoneId));

        var completion = new TaskCompletionSource<AdRequestResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var listener = new RequestListener(zoneId, completion);
            global::IR.Tapsell.Sdk.Tapsell.RequestAd(
                GetContext(),
                zoneId,
                new global::IR.Tapsell.Sdk.TapsellAdRequestOptions(),
                listener);
        });

        return await completion.Task.ConfigureAwait(false);
    }

    private async Task ShowFullscreenAdAsync(
        string zoneId,
        string adId,
        AdShowOptions? options,
        bool raiseRewardedEvent,
        CancellationToken cancellationToken)
    {
        EnsureInitialized();
        cancellationToken.ThrowIfCancellationRequested();

        zoneId = NormalizeRequired(zoneId, nameof(zoneId));
        adId = NormalizeRequired(adId, nameof(adId));
        options ??= new AdShowOptions();

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var listener = new ShowListener(zoneId, adId, raiseRewardedEvent, Rewarded, completion);
            var showOptions = new global::IR.Tapsell.Sdk.TapsellShowOptions
            {
                BackDisabled = options.BackDisabled,
                ImmersiveMode = options.ImmersiveMode,
                RotationMode = MapRotationMode(options),
                ShowDialog = options.ShowDialog
            };

            global::IR.Tapsell.Sdk.Tapsell.ShowAd(GetActivity(), zoneId, adId, showOptions, listener);
        });

        await completion.Task.ConfigureAwait(false);
    }

    private async Task<NativeAdRequestResult> RequestNativeBannerAsync(
        string zoneId,
        NativeAdRequestOptions options,
        CancellationToken cancellationToken)
    {
        var adIds = new List<string>(Math.Max(1, options.Count));

        for (var i = 0; i < Math.Max(1, options.Count); i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var request = await RequestSingleNativeBannerAsync(zoneId, cancellationToken).ConfigureAwait(false);
            adIds.Add(request);
        }

        return new NativeAdRequestResult(adIds, zoneId, NativeAdFormat.Banner, DateTimeOffset.UtcNow);
    }

    private async Task<string> RequestSingleNativeBannerAsync(string zoneId, CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var listener = new NativeBannerRequestListener(zoneId, completion, StoreNativeBannerAd);
            global::IR.Tapsell.Sdk.Nativeads.TapsellNativeBannerManager.GetAd(GetContext(), zoneId, listener);
        });

        return await completion.Task.ConfigureAwait(false);
    }

    private async Task<NativeAdRequestResult> RequestNativeVideoAsync(
        string zoneId,
        NativeAdRequestOptions options,
        CancellationToken cancellationToken)
    {
        var count = Math.Max(1, options.Count);
        var completion = new TaskCompletionSource<IReadOnlyList<string>>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var listener = new NativeVideoLoadListener(zoneId, count, completion, StoreNativeVideoAd);
            var builder = new global::IR.Tapsell.Sdk.Nativeads.TapsellNativeVideoAdLoader.Builder();
            builder.LoadMultipleAds(GetContext(), zoneId, count, listener);
        });

        var adIds = await completion.Task.ConfigureAwait(false);
        return new NativeAdRequestResult(adIds, zoneId, NativeAdFormat.Video, DateTimeOffset.UtcNow);
    }

    private async Task ShowNativeBannerAsync(string zoneId, NativeBannerAdState state, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var root = GetRootContent();
            var context = GetContext();
            var container = CreateOverlayContainer(GravityFlags.CenterHorizontal | GravityFlags.Bottom, matchParentWidth: true);
            root.AddView(container);

            var showListener = new NativeBannerShowListener();
            var builder = new global::IR.Tapsell.Sdk.Nativeads.TapsellNativeBannerManager.Builder();
            builder = builder.SetParentView(container)
                ?? throw new InvalidOperationException("Failed to configure native banner parent view.");
            builder = builder.WithShowListener(showListener)
                ?? throw new InvalidOperationException("Failed to configure native banner show listener.");

            var viewManager = builder.InflateTemplate(context)
                ?? throw new InvalidOperationException("Failed to create native banner template.");
            viewManager.NativeAdShowListener = showListener;

            global::IR.Tapsell.Sdk.Nativeads.TapsellNativeBannerManager.BindAd(
                context,
                viewManager,
                zoneId,
                state.AdId);

            state.Container = container;
        });
    }

    private async Task ShowNativeVideoAsync(string zoneId, NativeVideoAdState state, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var root = GetRootContent();
            var container = CreateOverlayContainer(GravityFlags.Center, matchParentWidth: true);
            root.AddView(container);

            var listener = new NativeVideoShowListener();
            state.Ad.SetShowListener(listener);
            state.Ad.AddToParentView(container);
            state.Container = container;
        });
    }

    private void StoreNativeBannerAd(string zoneId, string adId)
    {
        lock (_sync)
        {
            _nativeBannerAds[adId] = new NativeBannerAdState(zoneId, adId);
        }
    }

    private string StoreNativeVideoAd(string zoneId, global::IR.Tapsell.Sdk.Nativeads.TapsellNativeVideoAd ad)
    {
        var adId = Guid.NewGuid().ToString("N");

        lock (_sync)
        {
            _nativeVideoAds[adId] = new NativeVideoAdState(zoneId, adId, ad);
        }

        return adId;
    }

    private void EnsureInitialized()
    {
        if (!IsInitialized)
        {
            throw new InvalidOperationException("Tapsell is not initialized. Call InitializeAsync first.");
        }
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", paramName);
        }

        return value;
    }

    private static global::Android.App.Application GetApplication()
        => global::Android.App.Application.Context as global::Android.App.Application
           ?? throw new InvalidOperationException("Android application instance is not available.");

    private static Context GetContext()
        => Platform.CurrentActivity ?? global::Android.App.Application.Context
           ?? throw new InvalidOperationException("Android context is not available.");

    private static Activity GetActivity()
        => Platform.CurrentActivity
           ?? throw new InvalidOperationException("A foreground Android activity is required.");

    private static FrameLayout GetRootContent()
        => GetActivity().FindViewById<FrameLayout>(Android.Resource.Id.Content)
           ?? throw new InvalidOperationException("Android root content view is not available.");

    private static FrameLayout CreateOverlayContainer(GravityFlags gravity, bool matchParentWidth, bool matchParentHeight = false)
    {
        var context = GetContext();
        var container = new FrameLayout(context)
        {
            Clickable = false,
            Focusable = false
        };

        var layoutParams = new FrameLayout.LayoutParams(
            matchParentWidth ? ViewGroup.LayoutParams.MatchParent : ViewGroup.LayoutParams.WrapContent,
            matchParentHeight ? ViewGroup.LayoutParams.MatchParent : ViewGroup.LayoutParams.WrapContent,
            gravity);

        container.LayoutParameters = layoutParams;
        return container;
    }

    private static global::IR.Tapsell.Sdk.Bannerads.TapsellBannerType MapBannerType(BannerAdSize size)
        => size switch
        {
            BannerAdSize.Banner320x50 => global::IR.Tapsell.Sdk.Bannerads.TapsellBannerType.BANNER320x50!,
            BannerAdSize.Banner320x100 => global::IR.Tapsell.Sdk.Bannerads.TapsellBannerType.BANNER320x100!,
            BannerAdSize.Banner250x250 => global::IR.Tapsell.Sdk.Bannerads.TapsellBannerType.BANNER250x250!,
            BannerAdSize.Banner300x250 => global::IR.Tapsell.Sdk.Bannerads.TapsellBannerType.BANNER300x250!,
            _ => global::IR.Tapsell.Sdk.Bannerads.TapsellBannerType.BANNER320x50!
        };

    private static int MapRotationMode(AdShowOptions options)
        => options.RotationMode
            ? global::IR.Tapsell.Sdk.TapsellShowOptions.RotationUnlocked
            : global::IR.Tapsell.Sdk.TapsellShowOptions.RotationLockedPortrait;

    private sealed class RequestListener : global::IR.Tapsell.Sdk.TapsellAdRequestListener
    {
        private readonly string _zoneId;
        private readonly TaskCompletionSource<AdRequestResult> _completion;

        public RequestListener(string zoneId, TaskCompletionSource<AdRequestResult> completion)
        {
            _zoneId = zoneId;
            _completion = completion;
        }

        public override void OnAdAvailable(string? adId)
        {
            if (string.IsNullOrWhiteSpace(adId))
            {
                _completion.TrySetException(new InvalidOperationException("Tapsell returned an empty ad id."));
                return;
            }

            _completion.TrySetResult(new AdRequestResult(adId, _zoneId, DateTimeOffset.UtcNow));
        }

        public override void OnError(string? message)
            => _completion.TrySetException(new InvalidOperationException(message ?? "Tapsell request failed."));

        [Obsolete]
        public override void OnNoAdAvailable()
            => _completion.TrySetException(new InvalidOperationException("No ad is available for this zone."));

        [Obsolete]
        public override void OnNoNetwork()
            => _completion.TrySetException(new InvalidOperationException("No network connection is available."));
    }

    private sealed class ShowListener : global::IR.Tapsell.Sdk.TapsellAdShowListener
    {
        private readonly string _zoneId;
        private readonly string _adId;
        private readonly bool _raiseRewardedEvent;
        private readonly EventHandler<TapsellRewardedEventArgs>? _rewarded;
        private readonly TaskCompletionSource _completion;

        public ShowListener(
            string zoneId,
            string adId,
            bool raiseRewardedEvent,
            EventHandler<TapsellRewardedEventArgs>? rewarded,
            TaskCompletionSource completion)
        {
            _zoneId = zoneId;
            _adId = adId;
            _raiseRewardedEvent = raiseRewardedEvent;
            _rewarded = rewarded;
            _completion = completion;
        }

        public override void OnOpened()
        {
        }

        public override void OnClosed()
            => _completion.TrySetResult();

        public override void OnError(string? message)
            => _completion.TrySetException(new InvalidOperationException(message ?? "Tapsell show failed."));

        public override void OnRewarded(bool completed)
        {
            if (_raiseRewardedEvent)
            {
                _rewarded?.Invoke(this, new TapsellRewardedEventArgs(_zoneId, _adId, completed));
            }
        }

        public override void OnAdClicked()
        {
        }
    }

    private sealed class BannerListener : Java.Lang.Object, global::IR.Tapsell.Sdk.Bannerads.ITapsellBannerViewEventListener
    {
        private readonly TaskCompletionSource _completion;

        public BannerListener(TaskCompletionSource completion)
        {
            _completion = completion;
        }

        public void OnAdClicked()
        {
        }

        public void OnError(string? p0)
            => _completion.TrySetException(new InvalidOperationException(p0 ?? "Banner request failed."));

        public void OnHideBannerView()
        {
        }

        public void OnNoAdAvailable()
            => _completion.TrySetException(new InvalidOperationException("No banner ad is available for this zone."));

        public void OnNoNetwork()
            => _completion.TrySetException(new InvalidOperationException("No network connection is available."));

        public void OnRequestFilled()
            => _completion.TrySetResult();
    }

    private sealed class NativeBannerRequestListener : global::IR.Tapsell.Sdk.TapsellAdRequestListener
    {
        private readonly string _zoneId;
        private readonly TaskCompletionSource<string> _completion;
        private readonly Action<string, string> _store;

        public NativeBannerRequestListener(string zoneId, TaskCompletionSource<string> completion, Action<string, string> store)
        {
            _zoneId = zoneId;
            _completion = completion;
            _store = store;
        }

        public override void OnAdAvailable(string? adId)
        {
            if (string.IsNullOrWhiteSpace(adId))
            {
                _completion.TrySetException(new InvalidOperationException("Native banner request returned an empty ad id."));
                return;
            }

            _store(_zoneId, adId);
            _completion.TrySetResult(adId);
        }

        public override void OnError(string? message)
            => _completion.TrySetException(new InvalidOperationException(message ?? "Native banner request failed."));

        [Obsolete]
        public override void OnNoAdAvailable()
            => _completion.TrySetException(new InvalidOperationException("No native banner ad is available for this zone."));

        [Obsolete]
        public override void OnNoNetwork()
            => _completion.TrySetException(new InvalidOperationException("No network connection is available."));
    }

    private sealed class NativeVideoLoadListener : Java.Lang.Object, global::IR.Tapsell.Sdk.Nativeads.ITapsellNativeVideoAdLoadListener
    {
        private readonly string _zoneId;
        private readonly int _expectedCount;
        private readonly TaskCompletionSource<IReadOnlyList<string>> _completion;
        private readonly Func<string, global::IR.Tapsell.Sdk.Nativeads.TapsellNativeVideoAd, string> _store;
        private readonly List<string> _adIds = new();

        public NativeVideoLoadListener(
            string zoneId,
            int expectedCount,
            TaskCompletionSource<IReadOnlyList<string>> completion,
            Func<string, global::IR.Tapsell.Sdk.Nativeads.TapsellNativeVideoAd, string> store)
        {
            _zoneId = zoneId;
            _expectedCount = expectedCount;
            _completion = completion;
            _store = store;
        }

        public void OnError(string? p0)
            => _completion.TrySetException(new InvalidOperationException(p0 ?? "Native video request failed."));

        public void OnNoAdAvailable()
            => _completion.TrySetException(new InvalidOperationException("No native video ad is available for this zone."));

        public void OnNoNetwork()
            => _completion.TrySetException(new InvalidOperationException("No network connection is available."));

        public void OnRequestFilled(global::IR.Tapsell.Sdk.Nativeads.TapsellNativeVideoAd? p0)
        {
            if (p0 is null)
            {
                _completion.TrySetException(new InvalidOperationException("Native video request returned an empty ad."));
                return;
            }

            _adIds.Add(_store(_zoneId, p0));
            if (_adIds.Count >= _expectedCount)
            {
                _completion.TrySetResult(_adIds.ToArray());
            }
        }
    }

    private sealed class NativeBannerShowListener : Java.Lang.Object, global::IR.Tapsell.Sdk.Nativeads.INativeAdShowListener
    {
        public void OnAdClicked()
        {
        }

        public void OnShowFailure(Java.Lang.Exception? p0)
        {
        }
    }

    private sealed class NativeVideoShowListener : Java.Lang.Object, global::IR.Tapsell.Sdk.Nativeads.ITapsellNativeVideoAdShowListener
    {
        public void OnAdClicked()
        {
        }

        public void OnAdFinished(string? p0)
        {
        }
    }

    private sealed record BannerState(
        string ZoneId,
        FrameLayout Container,
        global::IR.Tapsell.Sdk.Bannerads.TapsellBannerView View,
        BannerListener Listener);

    private sealed record HostedBannerState(
        FrameLayout Container,
        global::IR.Tapsell.Sdk.Bannerads.TapsellBannerView View,
        BannerListener Listener);

    private sealed record HostedNativeState(
        FrameLayout Container,
        global::IR.Tapsell.Sdk.Nativeads.TapsellNativeVideoAd? VideoAd = null);

    private sealed record HostedPreRollState(
        FrameLayout Container,
        global::IR.Tapsell.Sdk.Preroll.TapsellPrerollAd? Ad);

    private sealed class NativeBannerAdState
    {
        public NativeBannerAdState(string zoneId, string adId)
        {
            ZoneId = zoneId;
            AdId = adId;
        }

        public string ZoneId { get; }

        public string AdId { get; }

        public FrameLayout? Container { get; set; }
    }

    private sealed class NativeVideoAdState
    {
        public NativeVideoAdState(string zoneId, string adId, global::IR.Tapsell.Sdk.Nativeads.TapsellNativeVideoAd ad)
        {
            ZoneId = zoneId;
            AdId = adId;
            Ad = ad;
        }

        public string ZoneId { get; }

        public string AdId { get; }

        public global::IR.Tapsell.Sdk.Nativeads.TapsellNativeVideoAd Ad { get; }

        public FrameLayout? Container { get; set; }
    }

    private sealed record PreRollState(
        string AdId,
        string ZoneId,
        PreRollRequestOptions? Options,
        FrameLayout? Container = null,
        FrameLayout? AdContainer = null,
        VideoView? VideoView = null,
        FrameLayout? CompanionContainer = null,
        global::IR.Tapsell.Sdk.Preroll.TapsellPrerollAd? Ad = null);
}
