# Tapsell Mediation App Key Guide

This guide explains how a consuming Android app should provide `TapsellMediationAppKey` when using `PersianAds.Tapsell`.

Short version:
- do not hardcode a real app key inside the library
- let the consuming app provide the value
- prefer runtime `options.AppId` initialization unless the native mediation SDK explicitly requires manifest metadata

## Native Gradle example

If you are looking at the original Android setup from Tapsell documentation, it typically looks like this in a Gradle app module:

```gradle
android {
    defaultConfig {
        manifestPlaceholders = [
            TapsellMediationAppKey: "YOUR_APP_ID",
        ]
    }
}
```

That Gradle snippet is useful as a reference because it shows the intent clearly: the host app supplies the value, and the manifest consumes it through a placeholder.

In a .NET MAUI library, you do not set this through Gradle directly. The equivalent package-side design is to let the consuming app set an MSBuild property, then forward it into `AndroidManifestPlaceholders`.

## Why the library should not set the key by itself

`TapsellMediationAppKey` is application-specific configuration.

That means the library cannot safely choose the correct value during install or package restore. Even if a package shipped a default placeholder like `YOUR_APP_ID`, the build could succeed while the mediation SDK would still fail at runtime.

The correct owner of this value is the final app that uses the SDK.

## Current status in this repository

Current implemented path:
- `PersianAds.Tapsell` already supports runtime initialization through `options.AppId` and `InitializeAsync(...)`

Not yet implemented in the library:
- automatic manifest placeholder wiring for `TapsellMediationAppKey`
- transitive MSBuild configuration that maps a consumer property into Android manifest placeholders

Because of that, treat manifest placeholder support as planned package behavior, not current integrated behavior.

## Recommended current local-project usage

If you are using this repository directly today, configure Tapsell through DI and initialize it in code.

Example `MauiProgram.cs`:

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
            options.DefaultInterstitialZoneId = "YOUR_INTERSTITIAL_ZONE_ID";
            options.DefaultBannerZoneId = "YOUR_BANNER_ZONE_ID";
            options.DefaultNativeZoneId = "YOUR_NATIVE_ZONE_ID";
            options.DefaultPreRollZoneId = "YOUR_PREROLL_ZONE_ID";
        });

        return builder.Build();
    }
}
```

Example initialization:

```csharp
using PersianAds.Models;
using PersianAds.Tapsell.Abstractions;

public sealed class AdsBootstrapper
{
    private readonly ITapsellService _tapsellService;

    public AdsBootstrapper(ITapsellService tapsellService)
    {
        _tapsellService = tapsellService;
    }

    public Task InitializeAsync()
    {
        return _tapsellService.InitializeAsync(new AdNetworkInitializationOptions(
            AppId: "YOUR_TAPSELL_APP_ID"));
    }
}
```

If the native SDK integration you are targeting works with runtime initialization alone, this is the preferred path.

## Planned package usage for manifest placeholder support

If manifest placeholder support is added later, the intended consumer experience should look like this:

1. The package contributes the manifest entry.
2. The consuming app sets one MSBuild property.
3. The package forwards that property into Android manifest placeholders.

Expected app-side configuration:

```xml
<PropertyGroup>
  <TapsellMediationAppKey>YOUR_APP_ID</TapsellMediationAppKey>
</PropertyGroup>
```

Expected Android manifest entry contributed by the package:

```xml
<meta-data
    android:name="TapsellMediationAppKey"
    android:value="${TapsellMediationAppKey}" />
```

Expected package build logic:

```xml
<PropertyGroup Condition="'$(TargetPlatformIdentifier)' == 'android' or '$(TargetFrameworkIdentifier)' == 'MonoAndroid'">
  <AndroidManifestPlaceholders>
    $(AndroidManifestPlaceholders);TapsellMediationAppKey=$(TapsellMediationAppKey)
  </AndroidManifestPlaceholders>
</PropertyGroup>
```

Important:
- this is the intended design if the repository adds manifest placeholder support
- this is not the current implemented behavior in `Tapsell/`
- conceptually, this is the MAUI/MSBuild equivalent of the Gradle `manifestPlaceholders` command shown earlier

## What consumers should do today

Use this decision rule:

- if Tapsell only needs an app ID at runtime, set `options.AppId` and call `InitializeAsync(...)`
- if your exact mediation setup requires `TapsellMediationAppKey` in the manifest, you currently need library changes before this repository can provide that automatically

## Notes for future implementation

When this feature is implemented in the package, the docs should be updated to confirm:
- which package version first supports `TapsellMediationAppKey`
- whether project references and NuGet package references both support the same flow
- whether runtime `options.AppId` is still required in addition to manifest metadata
