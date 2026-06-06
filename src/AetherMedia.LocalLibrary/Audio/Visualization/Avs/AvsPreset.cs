// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization.Avs;

/// <summary>
/// A parsed AVS (Advanced Visualization Studio) preset. The original
/// <c>.avs</c> file is a Nullsoft binary blob: 32-byte ASCII header
/// (<c>Nullsoft AVS Preset 0.2\x1A</c>), then a sequence of effect blocks
/// each prefixed by a 4-byte type ID and a length. The detailed effect
/// graph is preserved opaquely in <see cref="EffectBlobs"/>; v1 of the
/// renderer composes an AVS-styled output without executing every
/// individual effect.
/// </summary>
public sealed record AvsPreset(
    string FormatVersion,
    bool ClearEveryFrame,
    IReadOnlyList<AvsEffectBlob> EffectBlobs);

/// <summary>An opaque effect entry from a parsed AVS preset.</summary>
public sealed record AvsEffectBlob(int TypeCode, byte[] Payload);
