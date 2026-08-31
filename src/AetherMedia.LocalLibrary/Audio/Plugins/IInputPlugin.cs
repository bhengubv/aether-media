// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Output;

namespace AetherMedia.LocalLibrary.Audio.Plugins;

/// <summary>
/// Plugin that decodes a file format into PCM frames — the Winamp
/// <c>in_</c> plugin contract. Hosts register multiple input plugins;
/// each claims its file extensions through <see cref="SupportedExtensions"/>.
/// </summary>
public interface IInputPlugin
{
    /// <summary>Stable identifier.</summary>
    string Id { get; }

    /// <summary>Human-readable name.</summary>
    string DisplayName { get; }

    /// <summary>File extensions (without dot) this plugin can decode.</summary>
    IReadOnlyList<string> SupportedExtensions { get; }

    /// <summary>True when the plugin can decode the file at <paramref name="filePath"/>.</summary>
    bool CanDecode(string filePath);

    /// <summary>Open the file and report its format.</summary>
    Task<AudioFormat> OpenAsync(string filePath, CancellationToken ct = default);

    /// <summary>Pull the next chunk of interleaved PCM. Returns 0 at end-of-stream.</summary>
    int ReadSamples(Memory<float> destination);

    /// <summary>Close any open file / decoder state.</summary>
    void Close();

    // ── Added for the player engine. Both are DEFAULT members returning
    //    "can't do that" / "don't know", so every implementation that predates
    //    them keeps compiling and simply declines. ──────────────────────────

    /// <summary>
    /// Total length of the open source, or null when the decoder cannot say.
    /// Streams legitimately don't know; a file normally does. The engine falls
    /// back to counting samples when this is null, which gives an honest
    /// elapsed time but no total — better than inventing one.
    /// </summary>
    long? DurationMs => null;

    /// <summary>
    /// Jump to an absolute position. Returns the position actually reached,
    /// which is rarely the one asked for: compressed formats can only seek to
    /// a frame boundary. Returns null when this decoder cannot seek at all,
    /// and the caller must not treat that as an error — a live stream has
    /// nowhere to seek to.
    /// </summary>
    Task<long?> SeekAsync(long positionMs, CancellationToken ct = default)
        => Task.FromResult<long?>(null);
}
