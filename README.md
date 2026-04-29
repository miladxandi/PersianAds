# PersianAds

`PersianAds` is a .NET MAUI ad SDK workspace for Persian ad networks.

Right now the implemented provider is:
- Tapsell for Android

Planned next:
- Tapsell Plus
- Adivery
- iOS support after the Android API is stable

## Repository structure

- `PersianAds/`: shared provider-agnostic contracts and models
- `PersianAds.Tapsell.Binding/`: Android binding project for the native Tapsell SDK
- `Tapsell/`: .NET MAUI-facing Tapsell wrapper package
- `docs/`: extra integration notes
- `AGENTS.md`: repo-specific implementation and delivery rules

## Current implementation status

Implemented in `Tapsell/` for Android:
- SDK initialization
- user consent / GDPR consent forwarding
- rewarded ads
- interstitial ads
- banner ads
- native banner ads
- native video ads
- pre-roll ads
- VAST URL retrieval for pre-roll

Important current limitation:
- banner, native, and pre-roll hosting are currently shown through Android activity-root overlays from the service layer
- this works for integration, but it is not yet a final polished MAUI host-view API

## Build status

Current local build status:
- `PersianAds.Tapsell.Binding` builds successfully
- `Tapsell` builds successfully

## Installation

There are two ways to use this project.

### 1. Local project reference

If you are using this repository directly, add a project reference to `Tapsell/Tapsell.csproj` in your MAUI app.

### 2. Future NuGet usage

When the package is published, usage is intended to look like this:

```xml
<ItemGroup>
  <PackageReference Include="PersianAds.Tapsell" Version="<version>" />
</ItemGroup>
```

If you are not consuming a published package yet, use the local project approach for now.

## Android requirements

Your app must target Android through .NET MAUI.

The wrapper project already contains the required Android manifest/resource setup for the Tapsell SDK, including:
- network security config
- `com.google.android.gms.permission.AD_ID`

If you package this as a NuGet later, these assets should flow from the package. If you reference the project directly, they are already included by `Tapsell/Tapsell.csproj`.

## Register Tapsell in `MauiProgram.cs`

```csharp
using PersianAds.Tapsell;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>();

        builder.Services.AddTapsell(options =>
        {
            options.AppId = "YOUR_TAPSELL_APP_ID";
            options.EnableDebugMode = true;

            options.DefaultRewardedZoneId = "YOUR_REWARDED_ZONE_ID";
            options.DefaultInterstitialZoneId = "YOUR_INTERSTITIAL_ZONE_ID";
            options.DefaultBannerZoneId = "YOUR_BANNER_ZONE_ID";
            options.DefaultNativeZoneId = "YOUR_NATIVE_ZONE_ID";
            options.DefaultPreRollZoneId = "YOUR_PREROLL_ZONE_ID";
        });

        return builder.Build();
    }
}
```

## Core service API

Resolve `ITapsellService` from DI:

```csharp
using PersianAds.Tapsell.Abstractions;
```

The main Tapsell-specific members are:
- `InitializeAsync(...)`
- `SetInitializationListener(...)`
- `SetUserConsent(...)`
- `RequestRewardedAsync(...)`
- `ShowRewardedAsync(...)`
- `RequestInterstitialAsync(...)`
- `ShowInterstitialAsync(...)`
- `LoadBannerAsync(...)`
- `ShowBannerAsync(...)`
- `HideBannerAsync(...)`
- `RequestNativeAsync(...)`
- `ShowNativeAsync(...)`
- `RequestPreRollAsync(...)`
- `GetPreRollVastUrlAsync(...)`
- `ShowPreRollAsync(...)`
- `DestroyPreRollAsync(...)`

## Initialization flow

This is the equivalent of the native Tapsell startup flow.

```csharp
using PersianAds.Models;
using PersianAds.Tapsell.Abstractions;

public sealed class AdsBootstrapper
{
    private readonly ITapsellService _tapsell;

    public AdsBootstrapper(ITapsellService tapsell)
    {
        _tapsell = tapsell;
    }

    public async Task InitializeAsync()
    {
        _tapsell.SetInitializationListener(() =>
        {
            System.Diagnostics.Debug.WriteLine("Tapsell initialization completed.");
        });

        _tapsell.SetUserConsent(true);

        await _tapsell.InitializeAsync(new AdNetworkInitializationOptions(
            AppId: "YOUR_TAPSELL_APP_ID"));
    }
}
```

## Rewarded ads

Rewarded ads are the most complete and most important flow in the current implementation.

### Code-behind example

```csharp
using PersianAds.Models;
using PersianAds.Tapsell.Abstractions;

public partial class RewardedPage : ContentPage
{
    private readonly ITapsellService _tapsell;

    public RewardedPage(ITapsellService tapsell)
    {
        InitializeComponent();
        _tapsell = tapsell;

        _tapsell.Rewarded += (_, e) =>
        {
            System.Diagnostics.Debug.WriteLine(
                $"Reward callback: zone={e.ZoneId}, adId={e.AdId}, completed={e.Completed}");
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _tapsell.SetUserConsent(true);

        await _tapsell.InitializeAsync(new AdNetworkInitializationOptions(
            AppId: "YOUR_TAPSELL_APP_ID"));
    }

    private async void OnShowRewardedClicked(object? sender, EventArgs e)
    {
        var zoneId = "YOUR_REWARDED_ZONE_ID";

        var request = await _tapsell.RequestRewardedAsync(zoneId);
        await _tapsell.ShowRewardedAsync(zoneId, request.AdId);
    }
}
```

### ViewModel example

```csharp
using System.Windows.Input;
using PersianAds.Models;
using PersianAds.Tapsell.Abstractions;

public sealed class RewardedViewModel
{
    private readonly ITapsellService _tapsell;

    public ICommand ShowRewardedCommand { get; }

    public RewardedViewModel(ITapsellService tapsell)
    {
        _tapsell = tapsell;

        _tapsell.Rewarded += (_, e) =>
        {
            System.Diagnostics.Debug.WriteLine(
                $"Reward granted: completed={e.Completed}, adId={e.AdId}");
        };

        ShowRewardedCommand = new Command(async () => await ShowRewardedAsync());
    }

    public async Task InitializeAsync()
    {
        _tapsell.SetUserConsent(true);

        await _tapsell.InitializeAsync(new AdNetworkInitializationOptions(
            AppId: "YOUR_TAPSELL_APP_ID"));
    }

    private async Task ShowRewardedAsync()
    {
        var zoneId = "YOUR_REWARDED_ZONE_ID";
        var request = await _tapsell.RequestRewardedAsync(zoneId);
        await _tapsell.ShowRewardedAsync(zoneId, request.AdId);
    }
}
```

## Interstitial ads

Interstitial uses the same request/show pattern as rewarded.

```csharp
var zoneId = "YOUR_INTERSTITIAL_ZONE_ID";
var request = await tapsell.RequestInterstitialAsync(zoneId);
await tapsell.ShowInterstitialAsync(zoneId, request.AdId);
```

## Banner ads

Banner is currently managed by the service layer and shown as an Android overlay attached to the current activity root.

```csharp
await tapsell.LoadBannerAsync("YOUR_BANNER_ZONE_ID", BannerAdSize.Banner320x50);
await tapsell.ShowBannerAsync("YOUR_BANNER_ZONE_ID");

// later
await tapsell.HideBannerAsync("YOUR_BANNER_ZONE_ID");
```

Supported sizes:
- `BannerAdSize.Banner320x50`
- `BannerAdSize.Banner320x100`
- `BannerAdSize.Banner250x250`
- `BannerAdSize.Banner300x250`

## Native ads

Native ads support both banner and video formats.

### Native banner

```csharp
var result = await tapsell.RequestNativeAsync(
    "YOUR_NATIVE_ZONE_ID",
    new NativeAdRequestOptions(Format: NativeAdFormat.Banner, Count: 1));

var adId = result.AdIds[0];
await tapsell.ShowNativeAsync("YOUR_NATIVE_ZONE_ID", adId);
```

### Native video

```csharp
var result = await tapsell.RequestNativeAsync(
    "YOUR_NATIVE_ZONE_ID",
    new NativeAdRequestOptions(Format: NativeAdFormat.Video, Count: 1));

var adId = result.AdIds[0];
await tapsell.ShowNativeAsync("YOUR_NATIVE_ZONE_ID", adId);
```

Important note:
- native video ids are internal wrapper ids mapped to loaded native video objects
- you should request and show them in the same running app session

## Pre-roll ads

Pre-roll is also supported through the service layer and currently uses an Android overlay host.

### Request a pre-roll handle

```csharp
var request = await tapsell.RequestPreRollAsync(
    "YOUR_PREROLL_ZONE_ID",
    new PreRollRequestOptions(ContentUrl: "https://example.com/content.mp4"));
```

### Get VAST URL

```csharp
var vastUrl = await tapsell.GetPreRollVastUrlAsync(request.AdId);
```

### Show pre-roll

```csharp
await tapsell.ShowPreRollAsync(
    "YOUR_PREROLL_ZONE_ID",
    request.AdId,
    new PreRollShowOptions(
        ContentUrl: "https://example.com/content.mp4",
        HasCompanionBanner: true));
```

### Destroy pre-roll

```csharp
await tapsell.DestroyPreRollAsync(request.AdId);
```

## Recommended app startup pattern

A good production usage pattern is:
- call `SetUserConsent(...)` as early as possible
- call `InitializeAsync(...)` once during app startup
- request ads shortly before you need them
- show ads only after a successful request result
- keep your zone ids in configuration, not inline literals throughout the app

## Error handling

The wrapper throws managed exceptions when:
- the SDK is not initialized
- a zone id or ad id is missing
- the native SDK reports no ad available
- the native SDK reports no network
- the native SDK reports request/show failure

Example:

```csharp
try
{
    var request = await tapsell.RequestRewardedAsync("YOUR_REWARDED_ZONE_ID");
    await tapsell.ShowRewardedAsync("YOUR_REWARDED_ZONE_ID", request.AdId);
}
catch (Exception ex)
{
    System.Diagnostics.Debug.WriteLine(ex.Message);
}
```

## Production notes

Before shipping to production, you should validate:
- your real Tapsell app id
- your production zone ids
- consent flow in your app
- ad behavior on physical Android devices
- activity lifecycle behavior for banner/native/pre-roll overlays
- network behavior on weak and disconnected connections

## Known limitations

Current known limitations of this repository state:
- no iOS implementation yet
- no final MAUI host-view abstraction for banner/native/pre-roll yet
- no published NuGet package yet
- no sample app included in the repository yet
- native/pre-roll presentation is functional but still needs product-level UX hardening for a full production SDK release

## Docs and contribution notes

- contributor instructions: `AGENTS.md`
- extra integration notes: `docs/`

## Summary

If you are using this repository today, the main supported path is:
- Android
- MAUI
- local project reference
- Tapsell rewarded/interstitial/banner/native/pre-roll through `ITapsellService`
