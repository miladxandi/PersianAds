using Android.Widget;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Handlers;
using PersianAds.Models;
using PersianAds.Tapsell.Abstractions;
using PersianAds.Tapsell.Views;

namespace PersianAds.Tapsell.Platforms.Android;

public sealed class PreRollHostViewHandler : ViewHandler<TapsellPreRollHostView, FrameLayout>
{
    public static readonly IPropertyMapper<TapsellPreRollHostView, PreRollHostViewHandler> Mapper =
        new PropertyMapper<TapsellPreRollHostView, PreRollHostViewHandler>(ViewHandler.ViewMapper)
        {
            [nameof(TapsellPreRollHostView.ZoneId)] = MapAd,
            [nameof(TapsellPreRollHostView.AdId)] = MapAd,
            [nameof(TapsellPreRollHostView.ContentUrl)] = MapAd,
            [nameof(TapsellPreRollHostView.HasCompanionBanner)] = MapAd
        };

    public PreRollHostViewHandler() : base(Mapper)
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
            _ = service.DetachPreRollHostAsync(platformView);
        }

        base.DisconnectHandler(platformView);
    }

    public static void MapAd(PreRollHostViewHandler handler, TapsellPreRollHostView view)
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

        var options = new PreRollShowOptions(
            ContentUrl: VirtualView.ContentUrl,
            HasCompanionBanner: VirtualView.HasCompanionBanner);

        await service.AttachPreRollHostAsync(zoneId, adId, options, PlatformView).ConfigureAwait(false);
    }
}
