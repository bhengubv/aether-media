// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Plugins;

/// <summary>
/// Catch-all extension point — Winamp's <c>gen_</c> plugin contract. The
/// host calls <see cref="Initialize"/> when the player starts and
/// <see cref="Shutdown"/> when it exits; everything between is the
/// plugin's call. Example uses: tray icons, scrobbler bridges, OSC remote
/// controls, last.fm "love this track" toggles.
/// </summary>
public interface IGeneralPurposePlugin
{
    string Id { get; }
    string DisplayName { get; }

    Task InitializeAsync(IPluginHostServices services, CancellationToken ct = default);
    Task ShutdownAsync(CancellationToken ct = default);
}

/// <summary>
/// Services exposed to <see cref="IGeneralPurposePlugin"/> +
/// <see cref="ILibraryPlugin"/> implementations. Concrete services come
/// through the host's DI container.
/// </summary>
public interface IPluginHostServices
{
    /// <summary>Resolve a service of type <typeparamref name="T"/>; null when unavailable.</summary>
    T? GetService<T>() where T : class;
}
