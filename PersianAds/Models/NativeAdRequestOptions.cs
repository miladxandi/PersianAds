namespace PersianAds.Models;

public sealed record NativeAdRequestOptions(
    NativeAdFormat Format = NativeAdFormat.Banner,
    int Count = 1,
    IReadOnlyDictionary<string, string>? Extras = null);
