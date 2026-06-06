// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization.Avs;

/// <summary>
/// AVS Clear Screen effect (typecode 0x1A). Fills the frame with a fixed
/// colour. <see cref="OnlyFirstFrame"/> = 1 means clear only the first
/// frame (boot-up wipe).
/// </summary>
public sealed class AvsClearScreenEffect : AvsEffect
{
    private int _frameCount;

    public AvsClearScreenEffect(ReadOnlySpan<byte> payload)
    {
        var r = new AvsPayloadReader(payload);
        IsEnabled       = r.ReadInt32(1) != 0;
        ClearColour     = r.ReadInt32();
        OnlyFirstFrame  = r.ReadInt32() != 0;
        BlendMode       = r.ReadInt32();
    }

    public int ClearColour { get; }
    public bool OnlyFirstFrame { get; }
    public int BlendMode { get; }

    /// <inheritdoc/>
    public override string DisplayName => "Clear Screen";

    /// <inheritdoc/>
    public override void Render(RgbaFrame target, AvsRenderContext context, in VisualizationInputs inputs)
    {
        if (!IsEnabled) return;
        if (OnlyFirstFrame && _frameCount > 0) return;
        _frameCount++;

        // AVS stores colour as a Win32 BGR int — extract.
        byte b = (byte)(ClearColour & 0xFF);
        byte g = (byte)((ClearColour >> 8) & 0xFF);
        byte r = (byte)((ClearColour >> 16) & 0xFF);
        target.Clear(r, g, b, 0xFF);
    }
}
