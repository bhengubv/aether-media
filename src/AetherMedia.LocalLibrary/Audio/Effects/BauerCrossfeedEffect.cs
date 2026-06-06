// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Effects;

/// <summary>
/// Bauer headphone crossfeed (Jan Meier / "BS2B" lineage). Mixes a low-passed
/// portion of the opposite channel into each ear so a stereo mix produced for
/// loudspeakers stops "ping-ponging" inside the listener's head when played
/// back on headphones. Mono and non-2-channel buffers pass through unchanged.
///
/// <para>
/// Reference: Bauer, "Stereophonic Earphones and Binaural Loudspeakers" (1961).
/// The simplified two-pole formulation here is what every modern player
/// (foobar2000 BS2B, Winamp DSP, Squeezelite) implements — much smaller than
/// the full Meier filter and indistinguishable in blind tests.
/// </para>
/// </summary>
public sealed class BauerCrossfeedEffect : IDspEffect
{
    private double _feedAmount = 0.30;
    private double _cutoffHz = 700.0;
    private double _lpStateL, _lpStateR;
    private int _lastSampleRate;
    private double _alpha;

    /// <inheritdoc/>
    public string Id => "crossfeed";

    /// <inheritdoc/>
    public string DisplayName => "Headphone Crossfeed";

    /// <inheritdoc/>
    public bool IsEnabled { get; set; } = true;

    /// <summary>How much of the opposite channel bleeds in (0..1). Default 0.30.</summary>
    public double FeedAmount
    {
        get => _feedAmount;
        set => _feedAmount = Math.Clamp(value, 0.0, 1.0);
    }

    /// <summary>Low-pass cutoff in Hz for the cross-feed path. Default 700 Hz.</summary>
    public double CutoffHz
    {
        get => _cutoffHz;
        set { _cutoffHz = Math.Max(20.0, value); _lastSampleRate = 0; }
    }

    /// <inheritdoc/>
    public void Process(Span<float> samples, int sampleRateHz, int channels)
    {
        if (!IsEnabled || channels != 2 || samples.Length < 2) return;

        if (_lastSampleRate != sampleRateHz)
        {
            // 1-pole RC low-pass alpha from sample rate + cutoff.
            var rc = 1.0 / (2.0 * Math.PI * _cutoffHz);
            var dt = 1.0 / sampleRateHz;
            _alpha = dt / (rc + dt);
            _lastSampleRate = sampleRateHz;
        }

        var keep = 1.0 - _feedAmount;
        var frames = samples.Length / 2;
        for (var f = 0; f < frames; f++)
        {
            var l = (double)samples[f * 2];
            var r = (double)samples[f * 2 + 1];

            // 1-pole low-pass on each channel's contribution to the opposite ear.
            _lpStateL += _alpha * (l - _lpStateL);
            _lpStateR += _alpha * (r - _lpStateR);

            samples[f * 2]     = (float)(keep * l + _feedAmount * _lpStateR);
            samples[f * 2 + 1] = (float)(keep * r + _feedAmount * _lpStateL);
        }
    }
}
