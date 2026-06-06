// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Plugins;

/// <summary>
/// Loads + activates plugins. Implementations decide the discovery
/// mechanism — built-in (DI), filesystem scan, signed assembly load, etc.
/// </summary>
public interface IPluginHost
{
    IReadOnlyList<IInputPlugin>          InputPlugins   { get; }
    IReadOnlyList<IOutputPlugin>         OutputPlugins  { get; }
    IReadOnlyList<IGeneralPurposePlugin> GeneralPlugins { get; }
    IReadOnlyList<ILibraryPlugin>        LibraryPlugins { get; }

    /// <summary>
    /// Discover and load plugins from the configured source. Safe to call
    /// repeatedly; subsequent calls re-scan but do not duplicate already-
    /// loaded plugins.
    /// </summary>
    Task LoadAsync(CancellationToken ct = default);
}
