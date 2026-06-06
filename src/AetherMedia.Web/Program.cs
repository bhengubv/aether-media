// SPDX-License-Identifier: MIT

using AetherMedia.DependencyInjection;
using AetherMedia.UI.Shared;
using AetherMedia.UI.Shared.Services;
using AetherMedia.UI.Shared.ViewModels;
using AetherMedia.Web;

var builder = WebApplication.CreateBuilder(args);

// ── Blazor ──────────────────────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── Aether Media domain services ─────────────────────────────────────────
// LocalUhid identifies this node on the Aether mesh.  Configure via
// appsettings.json "AetherNetMedia:LocalUhid" or the AETHERNET_LOCAL_UHID env var.
var localUhid = builder.Configuration["AetherNetMedia:LocalUhid"]
             ?? Environment.GetEnvironmentVariable("AETHERNET_LOCAL_UHID")
             ?? "MEDIA-WEB01";

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

// ── Web-specific services ─────────────────────────────────────────────────
builder.Services.AddSingleton<IFilePicker, WebFilePicker>();

// ── Shared in-process media services ────────────────────────────────────
builder.Services.AddSingleton<LocalMediaPlayer>();
builder.Services.AddSingleton<LocalMediaFeed>();
builder.Services.AddSingleton<LocalCreatorChannel>();

builder.Services.AddSingleton<AetherMedia.Core.IMediaPlayer>(
    sp => sp.GetRequiredService<LocalMediaPlayer>());
builder.Services.AddSingleton<AetherMedia.Core.IMediaFeed>(
    sp => sp.GetRequiredService<LocalMediaFeed>());
builder.Services.AddSingleton<AetherMedia.Core.ICreatorChannel>(
    sp => sp.GetRequiredService<LocalCreatorChannel>());

// ── ViewModels — Scoped per Blazor circuit ────────────────────────────────
builder.Services.AddScoped<HomeViewModel>();
builder.Services.AddScoped<ReelFeedViewModel>();
builder.Services.AddScoped<NearbyViewModel>();
builder.Services.AddScoped<LibraryViewModel>();
builder.Services.AddScoped<PlayerViewModel>();
builder.Services.AddScoped<ProfileViewModel>();
builder.Services.AddScoped<MoreAppsViewModel>();
builder.Services.AddTransient<MetadataEditorViewModel>();
builder.Services.AddTransient<SubtitleSearchViewModel>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(AetherMedia.UI.Shared.ViewModels.ViewModelBase).Assembly);

app.Run();
