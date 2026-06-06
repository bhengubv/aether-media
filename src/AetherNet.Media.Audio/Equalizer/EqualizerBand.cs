// SPDX-License-Identifier: MIT

namespace AetherNet.Media.Audio.Equalizer;

/// <summary>
/// One band of a parametric equalizer.
/// </summary>
/// <param name="CenterFrequencyHz">Centre frequency of the band, in Hz.</param>
/// <param name="GainDb">Gain at the centre frequency, in dB (negative = cut).</param>
/// <param name="Q">
/// Quality factor — controls how narrow the band is. Lower Q (e.g. 0.7) gives
/// gentle, musical curves; higher Q (e.g. 4.0) gives surgical, narrow cuts.
/// </param>
public sealed record EqualizerBand(double CenterFrequencyHz, double GainDb, double Q);
