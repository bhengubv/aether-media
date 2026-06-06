// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization.Avs;

/// <summary>
/// AVS Blur effect (typecode 0x07). One of three 3×3 box-blur passes:
/// medium, light, or heavy. Mutates the frame in place.
/// </summary>
public sealed class AvsBlurEffect : AvsEffect
{
    /// <summary>Blur intensities documented in the AVS source.</summary>
    public enum BlurMode
    {
        Medium = 0,
        Light  = 1,
        Heavy  = 2,
    }

    public AvsBlurEffect(ReadOnlySpan<byte> payload)
    {
        var r = new AvsPayloadReader(payload);
        IsEnabled = r.ReadInt32(1) != 0;
        Mode = (BlurMode)r.ReadInt32();
        RoundMode = r.ReadInt32();
    }

    public BlurMode Mode { get; }
    public int RoundMode { get; }

    /// <inheritdoc/>
    public override string DisplayName => $"Blur ({Mode})";

    /// <inheritdoc/>
    public override void Render(RgbaFrame target, AvsRenderContext context, in VisualizationInputs inputs)
    {
        if (!IsEnabled) return;

        // 3×3 box blur with weight depending on mode. Allocate one scratch
        // buffer; one pass keeps the cost linear.
        var w = target.Width;
        var h = target.Height;
        var src = target.Pixels;
        var scratch = new byte[src.Length];
        Buffer.BlockCopy(src, 0, scratch, 0, src.Length);

        // Centre weight per mode. Medium = 4 (close to true 3x3 average),
        // Light = 6 (less mixing), Heavy = 2 (more mixing).
        var centreWeight = Mode switch
        {
            BlurMode.Light  => 6,
            BlurMode.Heavy  => 2,
            _               => 4,
        };
        var neighbourWeight = (9 - centreWeight) / 8.0;

        for (var y = 1; y < h - 1; y++)
        for (var x = 1; x < w - 1; x++)
        {
            var i = (y * w + x) * 4;
            for (var c = 0; c < 3; c++)
            {
                double sum =
                    centreWeight * scratch[i + c] +
                    neighbourWeight * (
                        scratch[i - 4 + c] + scratch[i + 4 + c] +
                        scratch[i - w * 4 + c] + scratch[i + w * 4 + c] +
                        scratch[i - w * 4 - 4 + c] + scratch[i - w * 4 + 4 + c] +
                        scratch[i + w * 4 - 4 + c] + scratch[i + w * 4 + 4 + c]);
                src[i + c] = (byte)Math.Clamp(sum / 9.0, 0, 255);
            }
        }
    }
}
