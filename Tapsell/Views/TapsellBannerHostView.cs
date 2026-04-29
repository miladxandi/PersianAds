using PersianAds.Models;

namespace PersianAds.Tapsell.Views;

public sealed class TapsellBannerHostView : View
{
    public static readonly BindableProperty ZoneIdProperty = BindableProperty.Create(
        nameof(ZoneId),
        typeof(string),
        typeof(TapsellBannerHostView),
        default(string));

    public static readonly BindableProperty AdSizeProperty = BindableProperty.Create(
        nameof(AdSize),
        typeof(BannerAdSize),
        typeof(TapsellBannerHostView),
        BannerAdSize.Banner320x50);

    public string? ZoneId
    {
        get => (string?)GetValue(ZoneIdProperty);
        set => SetValue(ZoneIdProperty, value);
    }

    public BannerAdSize AdSize
    {
        get => (BannerAdSize)GetValue(AdSizeProperty);
        set => SetValue(AdSizeProperty, value);
    }
}
