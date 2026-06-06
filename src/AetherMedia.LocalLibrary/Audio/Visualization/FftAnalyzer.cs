// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization;

/// <summary>
/// Pure-managed real-input FFT visualiser. Cooley–Tukey radix-2 with a Hann
/// window, returning linear magnitudes. Fast enough for real-time
/// visualisation at typical FFT sizes (1024 / 2048 / 4096).
/// </summary>
public sealed class FftAnalyzer : IVisualizationFeed
{
    private readonly int _fftSize;
    private readonly double[] _window;

    public FftAnalyzer(int fftSize = 2048)
    {
        if (fftSize < 64 || (fftSize & (fftSize - 1)) != 0)
            throw new ArgumentException("fftSize must be a power of two and ≥ 64.", nameof(fftSize));

        _fftSize = fftSize;
        _window = new double[fftSize];
        for (var i = 0; i < fftSize; i++)
            _window[i] = 0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / (fftSize - 1)));
    }

    /// <inheritdoc/>
    public int FftSize => _fftSize;

    /// <inheritdoc/>
    public SpectrumFrame? Analyse(ReadOnlySpan<float> samples, int sampleRateHz, int channels)
    {
        if (channels < 1) throw new ArgumentOutOfRangeException(nameof(channels));
        var frames = samples.Length / channels;
        if (frames < _fftSize) return null;

        // Downmix to mono + window
        var re = new double[_fftSize];
        var im = new double[_fftSize];
        float peak = 0f;
        for (var f = 0; f < _fftSize; f++)
        {
            double sum = 0;
            for (var c = 0; c < channels; c++)
            {
                var s = samples[f * channels + c];
                sum += s;
                var abs = Math.Abs(s);
                if (abs > peak) peak = abs;
            }
            re[f] = sum / channels * _window[f];
        }

        Fft(re, im);

        // Magnitude for 0..Nyquist
        var half = _fftSize / 2;
        var mags = new float[half];
        for (var k = 0; k < half; k++)
            mags[k] = (float)Math.Sqrt(re[k] * re[k] + im[k] * im[k]);

        return new SpectrumFrame(mags, peak, sampleRateHz);
    }

    /// <summary>In-place Cooley–Tukey radix-2 FFT.</summary>
    private void Fft(double[] re, double[] im)
    {
        var n = re.Length;
        // Bit-reversal permutation
        for (int i = 1, j = 0; i < n; i++)
        {
            var bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1)
                j ^= bit;
            j ^= bit;
            if (i < j)
            {
                (re[i], re[j]) = (re[j], re[i]);
                (im[i], im[j]) = (im[j], im[i]);
            }
        }
        // Cooley–Tukey
        for (var len = 2; len <= n; len <<= 1)
        {
            var ang = -2.0 * Math.PI / len;
            var wRe = Math.Cos(ang);
            var wIm = Math.Sin(ang);
            for (var i = 0; i < n; i += len)
            {
                double curRe = 1.0, curIm = 0.0;
                for (var k = 0; k < len / 2; k++)
                {
                    var tRe = curRe * re[i + k + len / 2] - curIm * im[i + k + len / 2];
                    var tIm = curRe * im[i + k + len / 2] + curIm * re[i + k + len / 2];
                    re[i + k + len / 2] = re[i + k] - tRe;
                    im[i + k + len / 2] = im[i + k] - tIm;
                    re[i + k] += tRe;
                    im[i + k] += tIm;
                    var newCurRe = curRe * wRe - curIm * wIm;
                    curIm = curRe * wIm + curIm * wRe;
                    curRe = newCurRe;
                }
            }
        }
    }
}
