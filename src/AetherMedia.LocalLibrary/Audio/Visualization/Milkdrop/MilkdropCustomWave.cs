// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization.Milkdrop;

/// <summary>
/// One custom wave definition. Milkdrop supports up to 4 (<c>wave_1_</c>..
/// <c>wave_4_</c>); each is a polyline whose vertex positions and colours
/// are driven by per_point equations applied to a configurable number of
/// audio samples (waveform or spectrum).
/// </summary>
public sealed record MilkdropCustomWave(
    int Index,
    bool Enabled,
    int Samples,
    int Separation,
    double Scaling,
    double Smoothing,
    double R, double G, double B, double A,
    bool Spectrum,
    bool UseDots,
    bool ThickOutline,
    bool Additive,
    IReadOnlyList<string> InitEquations,
    IReadOnlyList<string> PerFrameEquations,
    IReadOnlyList<string> PerPointEquations);
