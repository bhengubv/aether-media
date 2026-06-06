// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Plugins;

/// <summary>
/// Library extension — Winamp's <c>ml_</c> plugin contract. Provides custom
/// views into the library (e.g. "now playing graph", "rediscover" smart
/// view, "lyrics-driven mood search"). The plugin owns its view; the host
/// exposes services for query.
/// </summary>
public interface ILibraryPlugin
{
    string Id { get; }
    string DisplayName { get; }

    /// <summary>Name of the navigation entry shown in the host UI.</summary>
    string NavigationLabel { get; }

    Task InitializeAsync(IPluginHostServices services, CancellationToken ct = default);
    Task ShutdownAsync(CancellationToken ct = default);
}
