namespace PersianAds.Tapsell.Models;

public sealed class TapsellRewardedEventArgs : EventArgs
{
    public TapsellRewardedEventArgs(string zoneId, string adId, bool completed)
    {
        ZoneId = zoneId;
        AdId = adId;
        Completed = completed;
    }

    public string ZoneId { get; }

    public string AdId { get; }

    public bool Completed { get; }
}
