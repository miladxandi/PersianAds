# PersianAds Android Usage Guide

This guide shows how a MAUI Android app is expected to use `PersianAds.Tapsell`.

Important: the public API shown here is the intended consumer-facing shape of the SDK. If the NuGet package is not published yet, use a local project reference until packaging is ready.

## Package install

### Option 1: Install from NuGet

Use this when `PersianAds.Tapsell` is published.

```xml
<ItemGroup>
  <PackageReference Include="PersianAds.Tapsell" Version="<VERSION>" />
</ItemGroup>
```

Or by CLI:

```bash
dotnet add package PersianAds.Tapsell --version <VERSION>
```

### Option 2: Use a local project reference

Use this while developing the SDK locally.

```xml
<ItemGroup>
  <ProjectReference Include="..\PersianAds\Tapsell\Tapsell.csproj" />
</ItemGroup>
```

## Android requirements

The Tapsell package is designed for MAUI Android.

Make sure your app:

- targets Android with MAUI enabled
- has a valid Tapsell `AppId`
- uses valid zone IDs for each ad format
- requests the required ad formats from the Tapsell dashboard

## Register the SDK in `MauiProgram.cs`

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
            options.DefaultRewardedZoneId = "YOUR_REWARDED_ZONE_ID";
            options.DefaultNativeZoneId = "YOUR_NATIVE_ZONE_ID";
            options.DefaultPreRollZoneId = "YOUR_PREROLL_ZONE_ID";
            options.DefaultInterstitialZoneId = "YOUR_INTERSTITIAL_ZONE_ID";
            options.DefaultBannerZoneId = "YOUR_BANNER_ZONE_ID";
        });

        return builder.Build();
    }
}
```

## Initialize the SDK

Initialize the service once during app startup or before first ad usage.

```csharp
using PersianAds.Models;
using PersianAds.Tapsell.Abstractions;

public partial class App : Application
{
    public App(ITapsellService tapsellService)
    {
        InitializeComponent();
        MainPage = new AppShell();

        _ = InitializeAdsAsync(tapsellService);
    }

    private static async Task InitializeAdsAsync(ITapsellService tapsellService)
    {
        await tapsellService.InitializeAsync(new AdNetworkInitializationOptions(
            AppId: "YOUR_TAPSELL_APP_ID"));
    }
}
```

## Use from code-behind

This approach is useful when the page itself owns the ad flow.

### Rewarded ad example

```csharp
using PersianAds.Models;
using PersianAds.Tapsell.Abstractions;

public partial class RewardedPage : ContentPage
{
    private readonly ITapsellService _tapsellService;
    private string? _rewardedAdId;

    public RewardedPage(ITapsellService tapsellService)
    {
        InitializeComponent();
        _tapsellService = tapsellService;
    }

    private async void OnRequestRewardedClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await _tapsellService.RequestRewardedAsync(
                zoneId: string.Empty,
                options: new AdRequestOptions(UserId: "user-1001"));

            _rewardedAdId = result.AdId;
            StatusLabel.Text = $"Rewarded ad is ready: {_rewardedAdId}";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = ex.Message;
        }
    }

    private async void OnShowRewardedClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_rewardedAdId))
        {
            StatusLabel.Text = "Request a rewarded ad first.";
            return;
        }

        try
        {
            await _tapsellService.ShowRewardedAsync(
                zoneId: string.Empty,
                adId: _rewardedAdId,
                options: new AdShowOptions(ShowDialog: true));

            StatusLabel.Text = "Rewarded ad was shown.";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = ex.Message;
        }
    }
}
```

### Interstitial ad example

```csharp
private string? _interstitialAdId;

private async void OnRequestInterstitialClicked(object sender, EventArgs e)
{
    var result = await _tapsellService.RequestInterstitialAsync(string.Empty);
    _interstitialAdId = result.AdId;
}

private async void OnShowInterstitialClicked(object sender, EventArgs e)
{
    if (string.IsNullOrWhiteSpace(_interstitialAdId))
        return;

    await _tapsellService.ShowInterstitialAsync(string.Empty, _interstitialAdId);
}
```

### PreRoll ad example

```csharp
private string? _preRollAdId;

private async void OnRequestPreRollClicked(object sender, EventArgs e)
{
    var result = await _tapsellService.RequestPreRollAsync(
        string.Empty,
        new PreRollRequestOptions(ContentUrl: "https://example.com/content.mp4"));

    _preRollAdId = result.AdId;
}

private async void OnShowPreRollClicked(object sender, EventArgs e)
{
    if (string.IsNullOrWhiteSpace(_preRollAdId))
        return;

    await _tapsellService.ShowPreRollAsync(
        string.Empty,
        _preRollAdId,
        new PreRollShowOptions(ContentUrl: "https://example.com/content.mp4"));
}
```

## Use from a ViewModel

This approach is better when you want testable UI logic and command-based pages.

### Example ViewModel

```csharp
using System.Windows.Input;
using PersianAds.Models;
using PersianAds.Tapsell.Abstractions;

public class RewardedAdsViewModel : BindableObject
{
    private readonly ITapsellService _tapsellService;
    private string? _rewardedAdId;
    private string _status = "Idle";
    private bool _isBusy;

    public RewardedAdsViewModel(ITapsellService tapsellService)
    {
        _tapsellService = tapsellService;

        RequestRewardedCommand = new Command(async () => await RequestRewardedAsync(), () => !IsBusy);
        ShowRewardedCommand = new Command(async () => await ShowRewardedAsync(), () => !IsBusy);
    }

    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            OnPropertyChanged();
            ((Command)RequestRewardedCommand).ChangeCanExecute();
            ((Command)ShowRewardedCommand).ChangeCanExecute();
        }
    }

    public ICommand RequestRewardedCommand { get; }

    public ICommand ShowRewardedCommand { get; }

    private async Task RequestRewardedAsync()
    {
        try
        {
            IsBusy = true;
            Status = "Requesting rewarded ad...";

            var result = await _tapsellService.RequestRewardedAsync(
                string.Empty,
                new AdRequestOptions(UserId: "user-1001"));

            _rewardedAdId = result.AdId;
            Status = $"Rewarded ad ready: {_rewardedAdId}";
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ShowRewardedAsync()
    {
        if (string.IsNullOrWhiteSpace(_rewardedAdId))
        {
            Status = "Request a rewarded ad first.";
            return;
        }

        try
        {
            IsBusy = true;
            Status = "Showing rewarded ad...";

            await _tapsellService.ShowRewardedAsync(string.Empty, _rewardedAdId);
            Status = "Rewarded ad finished.";
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

### Register the ViewModel

```csharp
builder.Services.AddTransient<RewardedAdsViewModel>();
builder.Services.AddTransient<RewardedPage>();
```

### Use the ViewModel in a page

```csharp
public partial class RewardedPage : ContentPage
{
    public RewardedPage(RewardedAdsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
```

## Recommended format order

If you are implementing the SDK incrementally, start in this order:

1. Rewarded Ads
2. Native Ads
3. PreRoll Ads
4. Interstitial Ads
5. Banner Ads

## Zone ID strategy

You can either:

- pass a zone ID per call, or
- configure default zone IDs in `AddTapsell(...)` and pass `string.Empty`

Example:

```csharp
await _tapsellService.RequestRewardedAsync(string.Empty);
```

That means the service will use `DefaultRewardedZoneId`.

## Error handling

Always wrap ad calls with `try/catch` because ad loading and rendering depend on:

- valid app id and zone ids
- Android lifecycle state
- network availability
- native SDK availability
- ad inventory availability

## Current status note

This documentation describes the intended SDK usage shape for Android MAUI apps. If a specific ad format is still under implementation in the SDK, the public API should stay the same and only the internal native bridge should change.
