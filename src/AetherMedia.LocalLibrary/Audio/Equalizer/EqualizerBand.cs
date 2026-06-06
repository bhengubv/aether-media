// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Equalizer;

/// <summary>One band of a parametric equalizer.</summary>
/// <param name="CenterFrequencyHz">Centre frequency in Hz.</param>
/// <param name="GainDb">Gain at the centre frequency in dB (negative = cut).</param>
/// <param name="Q">
/// Quality factor. Lower Q (0.7) gives gentle, musical curves; higher Q (4.0)
/// gives narrow, surgical cuts.
/// </param>
public sealed record EqualizerBand(double CenterFrequencyHz, double GainDb, double Q);
