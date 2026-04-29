namespace PersianAds.Models;

public sealed record NativeAdRequestResult(
    IReadOnlyList<string> AdIds,
    string ZoneId,
    NativeAdFormat Format,
    DateTimeOffset RequestedAt,
    IReadOnlyDictionary<string, string>? Metadata = null);
