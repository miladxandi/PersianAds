using Android.Runtime;

namespace IR.Tapsell.Sdk
{
    public partial class TapsellAd
    {
        protected override Java.Lang.Object? RawAdSuggestion
        {
            get => AdSuggestion;
            set => AdSuggestion = value as IR.Tapsell.Sdk.Models.Suggestions.DirectAdSuggestion;
        }
    }
}

namespace IR.Tapsell.Sdk.Models.TapsellModel
{
    public partial class TapsellDirectAdModel
    {
        protected override Java.Lang.Object? RawAdSuggestion
        {
            get => AdSuggestion;
            set => AdSuggestion = value as IR.Tapsell.Sdk.Models.Suggestions.DirectAdSuggestion;
        }
    }

    public partial class TapsellNativeBannerAdModel
    {
        protected override Java.Lang.Object? RawAdSuggestion
        {
            get => AdSuggestion;
            set => AdSuggestion = value as IR.Tapsell.Sdk.Models.Suggestions.NativeBannerAdSuggestion;
        }
    }

    public partial class TapsellNativeVideoAdModel
    {
        protected override Java.Lang.Object? RawAdSuggestion
        {
            get => AdSuggestion;
            set => AdSuggestion = value as IR.Tapsell.Sdk.Models.Suggestions.NativeVideoAdSuggestion;
        }
    }
}
