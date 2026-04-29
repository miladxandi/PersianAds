namespace PersianAds.Models;

public sealed record AdRequestOptions(
    string? UserId = null,
    IReadOnlyDictionary<string, string>? Extras = null);
