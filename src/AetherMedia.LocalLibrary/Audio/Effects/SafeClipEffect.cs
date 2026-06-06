// SPDX-License-Identifier: MIT

using System.Numerics;

namespace AetherMedia.LocalLibrary.Audio.Effects;

/// <summary>
/// Final-stage hard clipper. Sits at the end of the chain so that any
/// pre-clipping gain (loudness normalisation, EQ boost) cannot push samples
/// past <see cref="CeilingDbfs"/>. Uses <see cref="Vector{T}"/> for a
/// SIMD-accelerated hot loop on net9+/net10 — the equivalent of Winamp's
/// Intel-IPP fast path, only portable.
/// </summary>
public sealed class SafeClipEffect : IDspEffect
{
    /// <summary>Clipping ceiling in dBFS. Default −0.3 dBFS leaves a little ISP headroom.</summary>
    public double CeilingDbfs { get; set; } = -0.3;

    /// <inheritdoc/>
    public string Id => "safe-clip";

    /// <inheritdoc/>
    public string DisplayName => "Safe Clip (Limiter)";

    /// <inheritdoc/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc/>
    public void Process(Span<float> samples, int sampleRateHz, int channels)
    {
        var ceiling = (float)Math.Pow(10.0, CeilingDbfs / 20.0);
        var negCeiling = -ceiling;

        // SIMD path
        if (Vector.IsHardwareAccelerated && samples.Length >= Vector<float>.Count)
        {
            var maxV = new Vector<float>(ceiling);
            var minV = new Vector<float>(negCeiling);
            var i = 0;
            for (; i <= samples.Length - Vector<float>.Count; i += Vector<float>.Count)
            {
                var v = new Vector<float>(samples[i..]);
                v = Vector.Min(maxV, Vector.Max(minV, v));
                v.CopyTo(samples[i..]);
            }
            // Tail
            for (; i < samples.Length; i++)
                samples[i] = Math.Clamp(samples[i], negCeiling, ceiling);
            return;
        }

        // Scalar fallback
        for (var i = 0; i < samples.Length; i++)
            samples[i] = Math.Clamp(samples[i], negCeiling, ceiling);
    }
}
