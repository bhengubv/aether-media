// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Effects;

/// <summary>
/// Voice-removal / karaoke effect. Lead vocals in commercial stereo mixes
/// are nearly always panned dead-centre, which means the signal is identical
/// in the left and right channel — subtracting one from the other cancels
/// the centre. Bass and kick drums are also commonly centred, so the
/// implementation high-passes the cancellation path: low frequencies (sub-
/// <see cref="LowBypassHz"/>) bypass the subtraction and stay in the mix.
///
/// <para>
/// Mono and non-2-channel buffers pass through unchanged. Output is stereo
/// — the cancelled signal is duplicated to both channels.
/// </para>
/// </summary>
public sealed class KaraokeEffect : IDspEffect
{
    private double _lowBypassHz = 150.0;
    private double _lpL, _lpR;
    private int _lastSampleRate;
    private double _alpha;

    /// <inheritdoc/>
    public string Id => "karaoke";

    /// <inheritdoc/>
    public string DisplayName => "Voice Removal (Karaoke)";

    /// <inheritdoc/>
    public bool IsEnabled { get; set; } = false;

    /// <summary>Frequencies below this cutoff bypass the L−R cancellation. Default 150 Hz.</summary>
    public double LowBypassHz
    {
        get => _lowBypassHz;
        set { _lowBypassHz = Math.Max(20.0, value); _lastSampleRate = 0; }
    }

    /// <inheritdoc/>
    public void Process(Span<float> samples, int sampleRateHz, int channels)
    {
        if (!IsEnabled || channels != 2 || samples.Length < 2) return;

        if (_lastSampleRate != sampleRateHz)
        {
            var rc = 1.0 / (2.0 * Math.PI * _lowBypassHz);
            var dt = 1.0 / sampleRateHz;
            _alpha = dt / (rc + dt);
            _lastSampleRate = sampleRateHz;
        }

        var frames = samples.Length / 2;
        for (var f = 0; f < frames; f++)
        {
            var l = (double)samples[f * 2];
            var r = (double)samples[f * 2 + 1];

            _lpL += _alpha * (l - _lpL);
            _lpR += _alpha * (r - _lpR);

            // Cancelled-centre signal = (L - R), divided by 2 to keep RMS sane.
            var cancelled = (l - r) * 0.5;
            // Add the low-frequency content back (it was mostly centre too, but
            // tonal balance survives because bass + kick drums get preserved).
            var lowMix = (_lpL + _lpR) * 0.5;
            var outSample = (float)(cancelled + lowMix);
            samples[f * 2]     = outSample;
            samples[f * 2 + 1] = outSample;
        }
    }
}
