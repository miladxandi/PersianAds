using Android.Runtime;
using IR.Tapsell.Sdk.Models.Suggestions;
using Java.Interop;

namespace IR.Tapsell.Sdk.Models.TapsellModel
{
    public partial class TapsellDirectAdModel
    {
        protected override Java.Lang.Object? RawAdSuggestion
        {
            [Register("getAdSuggestion", "()Lir/tapsell/sdk/models/suggestions/DirectAdSuggestion;", "")]
            get => AdSuggestion;
            [Register("setAdSuggestion", "(Lir/tapsell/sdk/models/suggestions/DirectAdSuggestion;)V", "")]
            set => AdSuggestion = value as DirectAdSuggestion;
        }
    }

    public partial class TapsellNativeBannerAdModel
    {
        protected override Java.Lang.Object? RawAdSuggestion
        {
            [Register("getAdSuggestion", "()Lir/tapsell/sdk/models/suggestions/NativeBannerAdSuggestion;", "")]
            get => AdSuggestion;
            [Register("setAdSuggestion", "(Lir/tapsell/sdk/models/suggestions/NativeBannerAdSuggestion;)V", "")]
            set => AdSuggestion = value as NativeBannerAdSuggestion;
        }
    }

    public partial class TapsellNativeVideoAdModel
    {
        protected override Java.Lang.Object? RawAdSuggestion
        {
            [Register("getAdSuggestion", "()Lir/tapsell/sdk/models/suggestions/NativeVideoAdSuggestion;", "")]
            get => AdSuggestion;
            [Register("setAdSuggestion", "(Lir/tapsell/sdk/models/suggestions/NativeVideoAdSuggestion;)V", "")]
            set => AdSuggestion = value as NativeVideoAdSuggestion;
        }
    }
}

namespace IR.Tapsell.Sdk
{
    public partial class TapsellAd
    {
        protected override Java.Lang.Object? RawAdSuggestion
        {
            [Register("getAdSuggestion", "()Lir/tapsell/sdk/models/suggestions/DirectAdSuggestion;", "")]
            get => AdSuggestion;
            [Register("setAdSuggestion", "(Lir/tapsell/sdk/models/suggestions/DirectAdSuggestion;)V", "")]
            set => AdSuggestion = value as DirectAdSuggestion;
        }
    }
}
