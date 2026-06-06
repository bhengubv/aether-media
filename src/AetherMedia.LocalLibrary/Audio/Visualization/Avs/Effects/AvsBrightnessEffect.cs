// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization.Avs;

/// <summary>
/// AVS Brightness / Tinting effect (typecode 0x17). Adds a signed
/// per-channel offset to every pixel.
/// </summary>
public sealed class AvsBrightnessEffect : AvsEffect
{
    public AvsBrightnessEffect(ReadOnlySpan<byte> payload)
    {
        var r = new AvsPayloadReader(payload);
        IsEnabled = r.ReadInt32(1) != 0;
        Mode      = r.ReadInt32(); // 0 = separate channels, 1 = uniform
        DeltaR    = r.ReadInt32();
        DeltaG    = r.ReadInt32();
        DeltaB    = r.ReadInt32();
        ExcludeColour = r.ReadInt32();
        ExcludeDistance = r.ReadInt32();
    }

    public int Mode { get; }
    public int DeltaR { get; }
    public int DeltaG { get; }
    public int DeltaB { get; }
    public int ExcludeColour { get; }
    public int ExcludeDistance { get; }

    /// <inheritdoc/>
    public override string DisplayName => "Brightness";

    /// <inheritdoc/>
    public override void Render(RgbaFrame target, AvsRenderContext context, in VisualizationInputs inputs)
    {
        if (!IsEnabled) return;
        var dr = Mode == 0 ? DeltaR : DeltaR;
        var dg = Mode == 0 ? DeltaG : DeltaR;
        var db = Mode == 0 ? DeltaB : DeltaR;

        var px = target.Pixels;
        for (var i = 0; i < px.Length; i += 4)
        {
            px[i]     = (byte)Math.Clamp(px[i]     + dr, 0, 255);
            px[i + 1] = (byte)Math.Clamp(px[i + 1] + dg, 0, 255);
            px[i + 2] = (byte)Math.Clamp(px[i + 2] + db, 0, 255);
        }
    }
}
