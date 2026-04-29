namespace PersianAds.Models;

public sealed record PreRollShowOptions(
    string? ContentUrl = null,
    bool HasCompanionBanner = false,
    IReadOnlyDictionary<string, string>? Extras = null);
