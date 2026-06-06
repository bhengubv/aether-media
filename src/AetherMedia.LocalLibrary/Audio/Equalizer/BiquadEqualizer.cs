// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Equalizer;

/// <summary>
/// 10-band parametric equalizer implemented as a cascade of peaking biquad
/// filters using the RBJ Audio EQ Cookbook formulas. Pure managed C#, no
/// external dependencies.
/// </summary>
public sealed class BiquadEqualizer : IEqualizer
{
    private readonly List<EqualizerBand> _bands = [];
    private Biquad[]? _filtersPerBand;
    private int _lastSampleRate;
    private int _lastChannels;

    /// <inheritdoc/>
    public string Id => "equalizer";

    /// <inheritdoc/>
    public string DisplayName => "Equalizer";

    /// <inheritdoc/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc/>
    public IReadOnlyList<EqualizerBand> Bands => _bands;

    /// <inheritdoc/>
    public IReadOnlyList<string> AvailablePresets =>
    [
        EqualizerPresets.Flat, EqualizerPresets.BassBoost, EqualizerPresets.TrebleBoost,
        EqualizerPresets.Rock, EqualizerPresets.Pop, EqualizerPresets.Jazz,
        EqualizerPresets.Classical, EqualizerPresets.Vocal,
    ];

    public BiquadEqualizer()
    {
        SetBands(EqualizerPresets.BandsFor(EqualizerPresets.Flat));
    }

    /// <inheritdoc/>
    public void SetBands(IEnumerable<EqualizerBand> bands)
    {
        ArgumentNullException.ThrowIfNull(bands);
        _bands.Clear();
        _bands.AddRange(bands);
        _filtersPerBand = null;
    }

    /// <inheritdoc/>
    public void ApplyPreset(string presetName) =>
        SetBands(EqualizerPresets.BandsFor(presetName));

    /// <inheritdoc/>
    public void Process(Span<float> samples, int sampleRateHz, int channels)
    {
        if (samples.Length == 0 || _bands.Count == 0) return;

        if (_filtersPerBand is null || _lastSampleRate != sampleRateHz || _lastChannels != channels)
            RebuildFilters(sampleRateHz, channels);

        var frames = samples.Length / channels;
        for (var f = 0; f < frames; f++)
        {
            for (var c = 0; c < channels; c++)
            {
                var idx = f * channels + c;
                double x = samples[idx];
                for (var b = 0; b < _bands.Count; b++)
                {
                    var filterIndex = b * channels + c;
                    x = _filtersPerBand![filterIndex].Process(x);
                }
                samples[idx] = (float)x;
            }
        }
    }

    private void RebuildFilters(int sampleRateHz, int channels)
    {
        _filtersPerBand = new Biquad[_bands.Count * channels];
        for (var b = 0; b < _bands.Count; b++)
        {
            var band = _bands[b];
            for (var c = 0; c < channels; c++)
            {
                _filtersPerBand[b * channels + c] =
                    Biquad.Peaking(band.CenterFrequencyHz, band.Q, band.GainDb, sampleRateHz);
            }
        }
        _lastSampleRate = sampleRateHz;
        _lastChannels = channels;
    }

    private struct Biquad
    {
        private double _b0, _b1, _b2, _a1, _a2;
        private double _x1, _x2, _y1, _y2;

        public static Biquad Peaking(double centerHz, double q, double gainDb, double sampleRateHz)
        {
            var a = Math.Pow(10.0, gainDb / 40.0);
            var w0 = 2.0 * Math.PI * centerHz / sampleRateHz;
            var cosW0 = Math.Cos(w0);
            var sinW0 = Math.Sin(w0);
            var alpha = sinW0 / (2.0 * q);

            var b0 = 1.0 + alpha * a;
            var b1 = -2.0 * cosW0;
            var b2 = 1.0 - alpha * a;
            var a0 = 1.0 + alpha / a;
            var a1 = -2.0 * cosW0;
            var a2 = 1.0 - alpha / a;

            return new Biquad
            {
                _b0 = b0 / a0, _b1 = b1 / a0, _b2 = b2 / a0,
                _a1 = a1 / a0, _a2 = a2 / a0,
            };
        }

        public double Process(double x)
        {
            var y = _b0 * x + _b1 * _x1 + _b2 * _x2 - _a1 * _y1 - _a2 * _y2;
            _x2 = _x1; _x1 = x;
            _y2 = _y1; _y1 = y;
            return y;
        }
    }
}
