namespace PersianAds.Models;

public sealed record AdShowOptions(
    bool BackDisabled = false,
    bool ImmersiveMode = true,
    bool RotationMode = false,
    bool ShowDialog = true,
    bool MuteVideo = false,
    IReadOnlyDictionary<string, string>? Extras = null);
