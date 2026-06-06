// SPDX-License-Identifier: MIT

using AetherMesh.Media.DependencyInjection;
using AetherMesh.Media.UI.Shared;
using AetherMesh.Media.UI.Shared.Services;
using AetherMesh.Media.UI.Shared.ViewModels;
using AetherMesh.Media.Web;

var builder = WebApplication.CreateBuilder(args);

// ── Blazor ──────────────────────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── Aether Media domain services ─────────────────────────────────────────
// LocalUhid identifies this node on the Aether mesh.  Configure via
// appsettings.json "AetherMeshMedia:LocalUhid" or the AETHERMESH_LOCAL_UHID env var.
var localUhid = builder.Configuration["AetherMeshMedia:LocalUhid"]
             ?? Environment.GetEnvironmentVariable("AETHERMESH_LOCAL_UHID")
             ?? "MEDIA-WEB01";

builder.Services.AddAetherMeshMedia(media =>
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

builder.Services.AddSingleton<AetherMesh.Media.Core.IMediaPlayer>(
    sp => sp.GetRequiredService<LocalMediaPlayer>());
builder.Services.AddSingleton<AetherMesh.Media.Core.IMediaFeed>(
    sp => sp.GetRequiredService<LocalMediaFeed>());
builder.Services.AddSingleton<AetherMesh.Media.Core.ICreatorChannel>(
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
    .AddAdditionalAssemblies(typeof(AetherMesh.Media.UI.Shared.ViewModels.ViewModelBase).Assembly);

app.Run();
