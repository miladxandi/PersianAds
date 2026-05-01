namespace PersianAds.Tapsell.Models;

public sealed class TapsellOptions
{
    public string AppId { get; set; } = string.Empty;

    public bool EnableDebugMode { get; set; }

    public string? DefaultRewardedZoneId { get; set; }
}
