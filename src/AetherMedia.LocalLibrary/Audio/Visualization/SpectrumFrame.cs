// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization;

/// <summary>
/// One frame of visualisation data: linear magnitude per FFT bin plus
/// peak amplitude, sufficient to drive a spectrum bar, a waveform line, or
/// (combined with a script) a Milkdrop-style visualiser.
/// </summary>
/// <param name="Magnitudes">
/// Linear magnitude (|X[k]|) for each frequency bin from 0 Hz to Nyquist.
/// Length is <c>fftSize/2</c>.
/// </param>
/// <param name="PeakAmplitude">Peak |sample| in the analysed time window.</param>
/// <param name="SampleRateHz">Sample rate used to interpret bin frequencies.</param>
public sealed record SpectrumFrame(
    float[] Magnitudes,
    float PeakAmplitude,
    int SampleRateHz)
{
    /// <summary>Frequency of bin <paramref name="binIndex"/> in Hz.</summary>
    public double BinFrequencyHz(int binIndex)
        => (double)binIndex * SampleRateHz / (Magnitudes.Length * 2);
}
