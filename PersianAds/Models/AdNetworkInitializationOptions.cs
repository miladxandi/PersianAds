namespace PersianAds.Models;

public sealed record AdNetworkInitializationOptions(
    string AppId,
    IReadOnlyDictionary<string, string>? Metadata = null);
