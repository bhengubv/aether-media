// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization.Avs;

/// <summary>
/// AVS Mosaic effect (typecode 0x1F). Pixelates the frame into square
/// blocks of <see cref="Quality"/> pixels — the lower the quality, the
/// chunkier the pixels.
/// </summary>
public sealed class AvsMosaicEffect : AvsEffect
{
    public AvsMosaicEffect(ReadOnlySpan<byte> payload)
    {
        var r = new AvsPayloadReader(payload);
        IsEnabled = r.ReadInt32(1) != 0;
        Quality   = Math.Clamp(r.ReadInt32(50), 1, 100);
    }

    public int Quality { get; }

    /// <inheritdoc/>
    public override string DisplayName => $"Mosaic (q={Quality})";

    /// <inheritdoc/>
    public override void Render(RgbaFrame target, AvsRenderContext context, in VisualizationInputs inputs)
    {
        if (!IsEnabled) return;
        var w = target.Width;
        var h = target.Height;
        var px = target.Pixels;

        // Block size: higher quality → smaller blocks. Documented mapping:
        // block = max(1, (100 - quality) / 2 + 1).
        var block = Math.Max(1, (100 - Quality) / 2 + 1);

        for (var by = 0; by < h; by += block)
        for (var bx = 0; bx < w; bx += block)
        {
            // Use the top-left pixel of the block as the block colour.
            var srcIdx = (by * w + bx) * 4;
            var r = px[srcIdx];
            var g = px[srcIdx + 1];
            var b = px[srcIdx + 2];
            var a = px[srcIdx + 3];
            for (var y = by; y < Math.Min(h, by + block); y++)
            for (var x = bx; x < Math.Min(w, bx + block); x++)
            {
                var di = (y * w + x) * 4;
                px[di]     = r;
                px[di + 1] = g;
                px[di + 2] = b;
                px[di + 3] = a;
            }
        }
    }
}
