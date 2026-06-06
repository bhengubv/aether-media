// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization.Avs;

/// <summary>
/// AVS Fadeout effect (typecode 0x04). Moves every pixel one step toward
/// the target <see cref="FadeColour"/> each frame; the step size is
/// determined by <see cref="FadeLength"/> (1 = fastest, 92 = slowest).
/// </summary>
public sealed class AvsFadeoutEffect : AvsEffect
{
    public AvsFadeoutEffect(ReadOnlySpan<byte> payload)
    {
        var r = new AvsPayloadReader(payload);
        IsEnabled  = r.ReadInt32(1) != 0;
        FadeLength = Math.Clamp(r.ReadInt32(8), 1, 92);
        FadeColour = r.ReadInt32();
    }

    public int FadeLength { get; }
    public int FadeColour { get; }

    /// <inheritdoc/>
    public override string DisplayName => "Fadeout";

    /// <inheritdoc/>
    public override void Render(RgbaFrame target, AvsRenderContext context, in VisualizationInputs inputs)
    {
        if (!IsEnabled) return;
        byte tb = (byte)(FadeColour & 0xFF);
        byte tg = (byte)((FadeColour >> 8) & 0xFF);
        byte tr = (byte)((FadeColour >> 16) & 0xFF);
        var step = Math.Max(1, 93 - FadeLength);

        var px = target.Pixels;
        for (var i = 0; i < px.Length; i += 4)
        {
            px[i]     = NudgeToward(px[i],     tr, step);
            px[i + 1] = NudgeToward(px[i + 1], tg, step);
            px[i + 2] = NudgeToward(px[i + 2], tb, step);
        }
    }

    private static byte NudgeToward(byte current, byte target, int step)
    {
        if (current > target) return (byte)Math.Max(target, current - step);
        if (current < target) return (byte)Math.Min(target, current + step);
        return current;
    }
}
