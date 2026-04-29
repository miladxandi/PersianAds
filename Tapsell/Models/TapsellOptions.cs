namespace PersianAds.Tapsell.Models;

public sealed class TapsellOptions
{
    public const string SectionName = "PersianAds:Tapsell";

    public string AppId { get; set; } = string.Empty;

    public bool EnableDebugMode { get; set; }

    public string? DefaultZoneId { get; set; }

    public string? DefaultRewardedZoneId { get; set; }

    public string? DefaultNativeZoneId { get; set; }

    public string? DefaultPreRollZoneId { get; set; }

    public string? DefaultInterstitialZoneId { get; set; }

    public string? DefaultBannerZoneId { get; set; }

    public string? MediationAdapterVersion { get; set; }
}
