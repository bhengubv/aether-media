// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization.Milkdrop;

/// <summary>
/// One custom shape definition extracted from a Milkdrop preset. Milkdrop
/// supports up to 4 shapes per preset (<c>shape_1_</c>..<c>shape_4_</c>).
/// Each shape can spawn many instances per frame; the per_frame equations
/// are re-run for every instance with the <c>instance</c> variable bound to
/// the loop counter.
/// </summary>
public sealed record MilkdropCustomShape(
    int Index,
    bool Enabled,
    int Instances,
    int Sides,
    bool Additive,
    bool ThickOutline,
    double X,
    double Y,
    double Radius,
    double Angle,
    double R, double G, double B, double A,
    double R2, double G2, double B2, double A2,
    double BorderR, double BorderG, double BorderB, double BorderA,
    IReadOnlyList<string> InitEquations,
    IReadOnlyList<string> PerFrameEquations);
