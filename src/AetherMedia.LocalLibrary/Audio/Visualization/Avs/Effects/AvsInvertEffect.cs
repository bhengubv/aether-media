// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization.Avs;

/// <summary>AVS Invert effect (typecode 0x26). Inverts every colour channel.</summary>
public sealed class AvsInvertEffect : AvsEffect
{
    public AvsInvertEffect(ReadOnlySpan<byte> payload)
    {
        var r = new AvsPayloadReader(payload);
        IsEnabled = r.ReadInt32(1) != 0;
    }

    /// <inheritdoc/>
    public override string DisplayName => "Invert";

    /// <inheritdoc/>
    public override void Render(RgbaFrame target, AvsRenderContext context, in VisualizationInputs inputs)
    {
        if (!IsEnabled) return;
        var px = target.Pixels;
        for (var i = 0; i < px.Length; i += 4)
        {
            px[i]     = (byte)(255 - px[i]);
            px[i + 1] = (byte)(255 - px[i + 1]);
            px[i + 2] = (byte)(255 - px[i + 2]);
        }
    }
}
