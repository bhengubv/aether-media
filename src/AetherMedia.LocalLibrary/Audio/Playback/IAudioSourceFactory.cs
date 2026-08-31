// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Plugins;

namespace AetherMedia.LocalLibrary.Audio.Playback;

/// <summary>
/// Hands the engine a FRESH decoder for a given file.
///
/// <para>Why a factory and not just an injected <see cref="IInputPlugin"/>: a decoder owns
/// per-file state — an open handle, a codec, a read position. One instance can therefore
/// decode exactly one file at a time. The engine needs two open simultaneously during a
/// crossfade (the track going out and the one coming in), so resolving a single shared
/// instance from DI would make crossfade impossible and gapless racy.</para>
///
/// <para>Implementations are platform-specific: each knows which decoders exist on this
/// device and news one up per call.</para>
/// </summary>
public interface IAudioSourceFactory
{
    /// <summary>
    /// Open a decoder for <paramref name="filePath"/>, or null when nothing on this device
    /// can decode it. Null is an honest "not supported here", not a failure — a head with no
    /// decoders at all (a web server) returns null for everything and the caller says so
    /// rather than appearing to do nothing.
    /// </summary>
    Task<IInputPlugin?> OpenAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Extensions this device can decode, lower-case and without the dot. Lets a library
    /// view grey out what it cannot play instead of finding out at the tap.
    /// </summary>
    IReadOnlyList<string> SupportedExtensions { get; }
}
