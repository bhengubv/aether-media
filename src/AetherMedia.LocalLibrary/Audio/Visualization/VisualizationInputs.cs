// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization;

/// <summary>
/// Per-frame inputs to a renderer. Time-domain samples drive oscilloscopes;
/// the spectrum drives bar / line analyzers. A renderer ignores whichever
/// input it doesn't need.
/// </summary>
/// <param name="TimeDomainSamples">Interleaved PCM samples for the current frame, in [-1,1].</param>
/// <param name="Spectrum">Optional FFT result from <see cref="FftAnalyzer"/>.</param>
/// <param name="SampleRateHz">Sample rate of <paramref name="TimeDomainSamples"/>.</param>
/// <param name="Channels">Channel count.</param>
public readonly record struct VisualizationInputs(
    ReadOnlyMemory<float> TimeDomainSamples,
    SpectrumFrame? Spectrum,
    int SampleRateHz,
    int Channels);
