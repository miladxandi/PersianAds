namespace PersianAds.Tapsell.Models;

public sealed record NativeAdRequestOptions(
    NativeAdFormat Format = NativeAdFormat.Banner,
    int Count = 1);
