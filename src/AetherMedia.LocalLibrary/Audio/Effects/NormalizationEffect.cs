// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Effects;

/// <summary>
/// Applies a fixed linear gain (typically the value returned by
/// <see cref="Loudness.LoudnessMeasurement.GainToTarget"/>) to every sample
/// in the buffer. This is how loudness measurements actually reach the
/// player output — the analyser produces a number; this effect spends it.
/// </summary>
public sealed class NormalizationEffect : IDspEffect
{
    /// <summary>Linear gain factor (1.0 = unchanged).</summary>
    public double Gain { get; set; } = 1.0;

    /// <inheritdoc/>
    public string Id => "normalize";

    /// <inheritdoc/>
    public string DisplayName => "Loudness Normalisation";

    /// <inheritdoc/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc/>
    public void Process(Span<float> samples, int sampleRateHz, int channels)
    {
        if (Gain == 1.0) return;
        var g = (float)Gain;
        for (var i = 0; i < samples.Length; i++)
            samples[i] *= g;
    }
}
