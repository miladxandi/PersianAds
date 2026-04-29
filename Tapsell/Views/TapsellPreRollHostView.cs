namespace PersianAds.Tapsell.Views;

public sealed class TapsellPreRollHostView : View
{
    public static readonly BindableProperty ZoneIdProperty = BindableProperty.Create(
        nameof(ZoneId),
        typeof(string),
        typeof(TapsellPreRollHostView),
        default(string));

    public static readonly BindableProperty AdIdProperty = BindableProperty.Create(
        nameof(AdId),
        typeof(string),
        typeof(TapsellPreRollHostView),
        default(string));

    public static readonly BindableProperty ContentUrlProperty = BindableProperty.Create(
        nameof(ContentUrl),
        typeof(string),
        typeof(TapsellPreRollHostView),
        default(string));

    public static readonly BindableProperty HasCompanionBannerProperty = BindableProperty.Create(
        nameof(HasCompanionBanner),
        typeof(bool),
        typeof(TapsellPreRollHostView),
        false);

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

    public string? ContentUrl
    {
        get => (string?)GetValue(ContentUrlProperty);
        set => SetValue(ContentUrlProperty, value);
    }

    public bool HasCompanionBanner
    {
        get => (bool)GetValue(HasCompanionBannerProperty);
        set => SetValue(HasCompanionBannerProperty, value);
    }
}
