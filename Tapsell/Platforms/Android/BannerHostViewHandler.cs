using Android.Widget;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Handlers;
using PersianAds.Tapsell.Abstractions;
using PersianAds.Tapsell.Views;

namespace PersianAds.Tapsell.Platforms.Android;

public sealed class BannerHostViewHandler : ViewHandler<TapsellBannerHostView, FrameLayout>
{
    public static readonly IPropertyMapper<TapsellBannerHostView, BannerHostViewHandler> Mapper =
        new PropertyMapper<TapsellBannerHostView, BannerHostViewHandler>(ViewHandler.ViewMapper)
        {
            [nameof(TapsellBannerHostView.ZoneId)] = MapAd,
            [nameof(TapsellBannerHostView.AdSize)] = MapAd
        };

    public BannerHostViewHandler() : base(Mapper)
    {
    }

    protected override FrameLayout CreatePlatformView()
        => new(MauiContext!.Context);

    protected override void ConnectHandler(FrameLayout platformView)
    {
        base.ConnectHandler(platformView);
        _ = UpdateAdAsync();
    }

    protected override void DisconnectHandler(FrameLayout platformView)
    {
        if (MauiContext?.Services.GetService<ITapsellService>() is TapsellService service)
        {
            _ = service.DetachBannerHostAsync(platformView);
        }

        base.DisconnectHandler(platformView);
    }

    public static void MapAd(BannerHostViewHandler handler, TapsellBannerHostView view)
        => _ = handler.UpdateAdAsync();

    private async Task UpdateAdAsync()
    {
        if (VirtualView?.ZoneId is not { Length: > 0 } zoneId || MauiContext is null)
        {
            return;
        }

        var service = MauiContext.Services.GetRequiredService<ITapsellService>() as TapsellService
            ?? throw new InvalidOperationException("Tapsell service is not registered.");

        await service.AttachBannerHostAsync(zoneId, VirtualView.AdSize, PlatformView).ConfigureAwait(false);
    }
}
