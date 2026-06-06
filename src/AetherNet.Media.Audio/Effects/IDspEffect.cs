// SPDX-License-Identifier: MIT

namespace AetherNet.Media.Audio.Effects;

/// <summary>
/// A single audio DSP effect that transforms PCM frames in place. Effects are
/// chained into an <see cref="IDspChain"/> and applied to every buffer the
/// player decodes before it reaches the output device — the Winamp
/// <c>dsp_</c> plugin contract, modernised for managed code.
/// </summary>
public interface IDspEffect
{
    /// <summary>Stable identifier (e.g. <c>"equalizer"</c>, <c>"compressor"</c>).</summary>
    string Id { get; }

    /// <summary>Human-readable name shown in the player's effects UI.</summary>
    string DisplayName { get; }

    /// <summary><c>true</c> when this effect is currently active in the chain.</summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Process one buffer of interleaved 32-bit float PCM in place.
    /// </summary>
    /// <param name="samples">
    /// Interleaved PCM samples to mutate in place. Layout is L R L R … for stereo.
    /// </param>
    /// <param name="sampleRateHz">Sample rate in Hz.</param>
    /// <param name="channels">Channel count.</param>
    void Process(Span<float> samples, int sampleRateHz, int channels);
}
