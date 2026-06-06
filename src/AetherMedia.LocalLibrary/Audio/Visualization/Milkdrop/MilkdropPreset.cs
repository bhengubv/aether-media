// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization.Milkdrop;

/// <summary>
/// A parsed Milkdrop <c>.milk</c> preset. Parameters are kept as raw
/// double-valued name → value pairs; the typed accessors live on
/// <see cref="MilkdropParameters"/>. Equation lists preserve the order
/// in which they appear in the file — Milkdrop evaluates per_frame_1
/// before per_frame_2, etc.
/// </summary>
public sealed record MilkdropPreset(
    string SectionName,
    IReadOnlyDictionary<string, double> Parameters,
    IReadOnlyList<string> PerFrameEquations,
    IReadOnlyList<string> PerPixelEquations);
