// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization.Avs;

/// <summary>
/// AVS Buffer Save effect (typecode 0x13). Stashes (or recalls) the current
/// frame in one of the 8 slots <see cref="AvsRenderContext"/> exposes — the
/// mechanism downstream effects use to layer different processed copies.
/// </summary>
public sealed class AvsBufferSaveEffect : AvsEffect
{
    /// <summary>What this instance does with the chosen slot.</summary>
    public enum Operation
    {
        Save     = 0,
        Restore  = 1,
        SaveAlphaBlend = 2,
    }

    public AvsBufferSaveEffect(ReadOnlySpan<byte> payload)
    {
        var r = new AvsPayloadReader(payload);
        IsEnabled = r.ReadInt32(1) != 0;
        Op        = (Operation)r.ReadInt32();
        Slot      = Math.Clamp(r.ReadInt32(1), 1, AvsRenderContext.BufferCount);
        BlendMode = r.ReadInt32();
    }

    public Operation Op { get; }
    public int Slot { get; }
    public int BlendMode { get; }

    /// <inheritdoc/>
    public override string DisplayName => $"BufferSave ({Op} {Slot})";

    /// <inheritdoc/>
    public override void Render(RgbaFrame target, AvsRenderContext context, in VisualizationInputs inputs)
    {
        if (!IsEnabled) return;
        switch (Op)
        {
            case Operation.Save:
                context.SaveBuffer(Slot, target);
                break;
            case Operation.Restore:
                var buf = context.GetBuffer(Slot);
                if (buf is not null && buf.Length == target.Pixels.Length)
                    Buffer.BlockCopy(buf, 0, target.Pixels, 0, buf.Length);
                break;
            case Operation.SaveAlphaBlend:
                // Blend current frame with the stored buffer at 50%.
                var existing = context.GetBuffer(Slot);
                if (existing is not null && existing.Length == target.Pixels.Length)
                {
                    var px = target.Pixels;
                    for (var i = 0; i < px.Length; i += 4)
                    {
                        px[i]     = (byte)((px[i]     + existing[i])     / 2);
                        px[i + 1] = (byte)((px[i + 1] + existing[i + 1]) / 2);
                        px[i + 2] = (byte)((px[i + 2] + existing[i + 2]) / 2);
                    }
                }
                context.SaveBuffer(Slot, target);
                break;
        }
    }
}
