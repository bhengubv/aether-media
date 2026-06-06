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
}
