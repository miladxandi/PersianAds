namespace PersianAds.Models;

public sealed record AdRequestResult(
    string AdId,
    string ZoneId,
    DateTimeOffset RequestedAt,
    IReadOnlyDictionary<string, string>? Metadata = null);
