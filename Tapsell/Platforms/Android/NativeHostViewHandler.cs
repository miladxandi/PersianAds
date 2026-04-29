using Android.Widget;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Handlers;
using PersianAds.Tapsell.Abstractions;
using PersianAds.Tapsell.Views;

namespace PersianAds.Tapsell.Platforms.Android;

public sealed class NativeHostViewHandler : ViewHandler<TapsellNativeHostView, FrameLayout>
{
    public static readonly IPropertyMapper<TapsellNativeHostView, NativeHostViewHandler> Mapper =
        new PropertyMapper<TapsellNativeHostView, NativeHostViewHandler>(ViewHandler.ViewMapper)
        {
            [nameof(TapsellNativeHostView.ZoneId)] = MapAd,
            [nameof(TapsellNativeHostView.AdId)] = MapAd,
            [nameof(TapsellNativeHostView.Format)] = MapAd
        };

    public NativeHostViewHandler() : base(Mapper)
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
            _ = service.DetachNativeHostAsync(platformView);
        }

        base.DisconnectHandler(platformView);
    }

    public static void MapAd(NativeHostViewHandler handler, TapsellNativeHostView view)
        => _ = handler.UpdateAdAsync();

    private async Task UpdateAdAsync()
    {
        if (VirtualView?.ZoneId is not { Length: > 0 } zoneId ||
            VirtualView.AdId is not { Length: > 0 } adId ||
            MauiContext is null)
        {
            return;
        }

        var service = MauiContext.Services.GetRequiredService<ITapsellService>() as TapsellService
            ?? throw new InvalidOperationException("Tapsell service is not registered.");

        await service.AttachNativeHostAsync(zoneId, adId, VirtualView.Format, PlatformView).ConfigureAwait(false);
    }
}
