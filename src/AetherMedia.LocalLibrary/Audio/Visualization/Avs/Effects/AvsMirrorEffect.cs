// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization.Avs;

/// <summary>
/// AVS Mirror effect (typecode 0x1B). Reflects the frame across one or
/// more axes. The <c>mode</c> bitmask documented in the AVS source:
/// <c>0x01</c> = horizontal (mirror left → right), <c>0x02</c> = vertical
/// (mirror top → bottom), <c>0x04</c> = left/right reverse, <c>0x08</c> =
/// top/bottom reverse.
/// </summary>
public sealed class AvsMirrorEffect : AvsEffect
{
    public AvsMirrorEffect(ReadOnlySpan<byte> payload)
    {
        var r = new AvsPayloadReader(payload);
        IsEnabled = r.ReadInt32(1) != 0;
        ModeBits  = r.ReadInt32();
    }

    public int ModeBits { get; }

    /// <inheritdoc/>
    public override string DisplayName => "Mirror";

    /// <inheritdoc/>
    public override void Render(RgbaFrame target, AvsRenderContext context, in VisualizationInputs inputs)
    {
        if (!IsEnabled) return;
        var w = target.Width;
        var h = target.Height;
        var px = target.Pixels;

        // Horizontal mirror (left → right): for x in [0, w/2), copy column x to (w-1-x).
        if ((ModeBits & 0x01) != 0)
        {
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w / 2; x++)
            {
                var i = (y * w + x) * 4;
                var j = (y * w + (w - 1 - x)) * 4;
                CopyPixel(px, i, j);
            }
        }
        // Vertical mirror (top → bottom): for y in [0, h/2), copy row y to (h-1-y).
        if ((ModeBits & 0x02) != 0)
        {
            for (var y = 0; y < h / 2; y++)
            for (var x = 0; x < w; x++)
            {
                var i = (y * w + x) * 4;
                var j = ((h - 1 - y) * w + x) * 4;
                CopyPixel(px, i, j);
            }
        }
    }

    private static void CopyPixel(byte[] px, int src, int dest)
    {
        px[dest]     = px[src];
        px[dest + 1] = px[src + 1];
        px[dest + 2] = px[src + 2];
        px[dest + 3] = px[src + 3];
    }
}
