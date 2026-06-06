// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Effects;

/// <summary>
/// Pre-amp gain stage. Multiplies every sample by a fixed factor derived
/// from <see cref="GainDb"/>. Lives first in a typical chain so the EQ /
/// limiter downstream see the corrected signal — matches the position of
/// Winamp's pre-amp slider in the legacy EQ window.
/// </summary>
public sealed class PreampEffect : IDspEffect
{
    private double _gainDb;
    private float _factor = 1.0f;

    /// <inheritdoc/>
    public string Id => "preamp";

    /// <inheritdoc/>
    public string DisplayName => "Pre-amp";

    /// <inheritdoc/>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Gain in dB. Negative attenuates, positive boosts. Default 0.</summary>
    public double GainDb
    {
        get => _gainDb;
        set
        {
            _gainDb = value;
            _factor = (float)Math.Pow(10.0, value / 20.0);
        }
    }

    /// <inheritdoc/>
    public void Process(Span<float> samples, int sampleRateHz, int channels)
    {
        if (!IsEnabled || Math.Abs(_factor - 1.0f) < 1e-6f) return;
        for (var i = 0; i < samples.Length; i++)
            samples[i] *= _factor;
    }
}
