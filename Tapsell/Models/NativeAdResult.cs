namespace PersianAds.Tapsell.Models;

public sealed record NativeAdResult(
    string ZoneId,
    IReadOnlyList<string> AdIds);
