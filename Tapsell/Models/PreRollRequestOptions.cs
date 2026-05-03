namespace PersianAds.Tapsell.Models;

public sealed record PreRollRequestOptions(
    string ContentUrl,
    bool HasCompanionBanner = false);
