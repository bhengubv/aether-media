// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization.Avs;

/// <summary>
/// AVS Comment effect (typecode 0x16). No render output — stores a string
/// the preset author can use for in-file notes.
/// </summary>
public sealed class AvsCommentEffect : AvsEffect
{
    public AvsCommentEffect(ReadOnlySpan<byte> payload)
    {
        var r = new AvsPayloadReader(payload);
        Text = r.ReadLengthPrefixedString();
    }

    public string Text { get; }

    /// <inheritdoc/>
    public override string DisplayName => "Comment";

    /// <inheritdoc/>
    public override void Render(RgbaFrame target, AvsRenderContext context, in VisualizationInputs inputs)
    {
        // No-op by design.
    }
}
