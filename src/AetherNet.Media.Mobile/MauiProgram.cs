// SPDX-License-Identifier: MIT

using AetherNet.Media.DependencyInjection;
using AetherNet.Media.Mobile.Services;
using AetherNet.Media.UI.Shared.Services;
using AetherNet.Media.UI.Shared.ViewModels;
using Microsoft.Extensions.Logging;
using AppIFilePicker = AetherNet.Media.UI.Shared.IFilePicker;

namespace AetherNet.Media.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // ── Blazor Hybrid ──────────────────────────────────────────────
        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // ── Aether Media domain services ───────────────────────────────
        // LocalUhid identifies this node on the Aether mesh.
        // Persisted in preferences so it's stable across app restarts.
        var localUhid = Preferences.Default.Get("LocalUhid", string.Empty);
        if (string.IsNullOrWhiteSpace(localUhid))
        {
            localUhid = "MEDIA-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            Preferences.Default.Set("LocalUhid", localUhid);
        }

        builder.Services.AddAetherNetMedia(media =>
        {
            media
                .AddSocial()
                .AddContent()
                .AddStreaming()
                .AddIdentity()
                .AddDistribution()
                .AddLocalLibrary()
                .AddAI()
                .AddReel(localUhid);
        });

        // ── Platform services ──────────────────────────────────────────
        builder.Services.AddSingleton<AppIFilePicker, MauiFilePicker>();

        // ── Shared in-process media services ──────────────────────────
        builder.Services.AddSingleton<LocalMediaPlayer>();
        builder.Services.AddSingleton<LocalMediaFeed>();
        builder.Services.AddSingleton<LocalCreatorChannel>();

        // Expose via interfaces
        builder.Services.AddSingleton<AetherNet.Media.Core.IMediaPlayer>(
            sp => sp.GetRequiredService<LocalMediaPlayer>());
        builder.Services.AddSingleton<AetherNet.Media.Core.IMediaFeed>(
            sp => sp.GetRequiredService<LocalMediaFeed>());
        builder.Services.AddSingleton<AetherNet.Media.Core.ICreatorChannel>(
            sp => sp.GetRequiredService<LocalCreatorChannel>());

        // ── ViewModels — Singleton in MAUI (single-app lifetime) ──────
        builder.Services.AddSingleton<HomeViewModel>();
        builder.Services.AddSingleton<ReelFeedViewModel>();
        builder.Services.AddSingleton<NearbyViewModel>();
        builder.Services.AddSingleton<LibraryViewModel>();
        builder.Services.AddSingleton<PlayerViewModel>();
        builder.Services.AddSingleton<ProfileViewModel>();
        builder.Services.AddSingleton<MoreAppsViewModel>();
        builder.Services.AddTransient<MetadataEditorViewModel>();
        builder.Services.AddTransient<SubtitleSearchViewModel>();

        return builder.Build();
    }
}
