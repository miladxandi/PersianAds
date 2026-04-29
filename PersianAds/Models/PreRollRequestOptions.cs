namespace PersianAds.Models;

public sealed record PreRollRequestOptions(
    string? ContentUrl = null,
    bool RequestVastOnly = false,
    IReadOnlyDictionary<string, string>? Extras = null);
