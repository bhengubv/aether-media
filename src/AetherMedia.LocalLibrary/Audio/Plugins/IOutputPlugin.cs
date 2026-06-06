// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Output;

namespace AetherMedia.LocalLibrary.Audio.Plugins;

/// <summary>
/// Plugin that produces an <see cref="IAudioOutput"/>. Mirrors Winamp's
/// <c>out_</c> plugin so future hosts can ship third-party PortAudio /
/// PulseAudio backends without editing the core library.
/// </summary>
public interface IOutputPlugin
{
    string Id { get; }
    string DisplayName { get; }

    /// <summary>True when the plugin can run on the current OS / hardware.</summary>
    bool IsAvailable { get; }

    /// <summary>Instantiate a fresh output instance.</summary>
    IAudioOutput Create();
}
