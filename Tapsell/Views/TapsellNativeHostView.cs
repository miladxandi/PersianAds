using PersianAds.Models;

namespace PersianAds.Tapsell.Views;

public sealed class TapsellNativeHostView : View
{
    public static readonly BindableProperty ZoneIdProperty = BindableProperty.Create(
        nameof(ZoneId),
        typeof(string),
        typeof(TapsellNativeHostView),
        default(string));

    public static readonly BindableProperty AdIdProperty = BindableProperty.Create(
        nameof(AdId),
        typeof(string),
        typeof(TapsellNativeHostView),
        default(string));

    public static readonly BindableProperty FormatProperty = BindableProperty.Create(
        nameof(Format),
        typeof(NativeAdFormat),
        typeof(TapsellNativeHostView),
        NativeAdFormat.Banner);

    public string? ZoneId
    {
        get => (string?)GetValue(ZoneIdProperty);
        set => SetValue(ZoneIdProperty, value);
    }

    public string? AdId
    {
        get => (string?)GetValue(AdIdProperty);
        set => SetValue(AdIdProperty, value);
    }

    public NativeAdFormat Format
    {
        get => (NativeAdFormat)GetValue(FormatProperty);
        set => SetValue(FormatProperty, value);
    }
}
