// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization.Avs;

/// <summary>
/// Instantiates the right <see cref="AvsEffect"/> subclass given a parsed
/// type code + payload. Unknown / unimplemented type codes fall through to
/// a no-op so a preset with unsupported effects still renders the rest of
/// its chain cleanly.
/// </summary>
public static class AvsEffectFactory
{
    /// <summary>Recognised type codes — exposed for diagnostics.</summary>
    public static IReadOnlySet<int> SupportedTypeCodes { get; } = new HashSet<int>
    {
        AvsTypeCode.Blur,
        AvsTypeCode.Mirror,
        AvsTypeCode.Mosaic,
        AvsTypeCode.Brightness,
        AvsTypeCode.Invert,
        AvsTypeCode.ClearScreen,
        AvsTypeCode.Fadeout,
        AvsTypeCode.Comment,
        AvsTypeCode.BufferSave,
        AvsTypeCode.SuperScope,
    };

    /// <summary>Construct an effect from a typecode + payload. Never returns null.</summary>
    public static AvsEffect Create(int typeCode, ReadOnlySpan<byte> payload) =>
        typeCode switch
        {
            AvsTypeCode.Blur        => new AvsBlurEffect(payload),
            AvsTypeCode.Mirror      => new AvsMirrorEffect(payload),
            AvsTypeCode.Mosaic      => new AvsMosaicEffect(payload),
            AvsTypeCode.Brightness  => new AvsBrightnessEffect(payload),
            AvsTypeCode.Invert      => new AvsInvertEffect(payload),
            AvsTypeCode.ClearScreen => new AvsClearScreenEffect(payload),
            AvsTypeCode.Fadeout     => new AvsFadeoutEffect(payload),
            AvsTypeCode.Comment     => new AvsCommentEffect(payload),
            AvsTypeCode.BufferSave  => new AvsBufferSaveEffect(payload),
            AvsTypeCode.SuperScope  => new AvsSuperScopeEffect(payload),
            _                        => new AvsUnknownEffect(typeCode),
        };
}

/// <summary>Placeholder for effect codes the runtime doesn't yet recognise — no-op render.</summary>
public sealed class AvsUnknownEffect : AvsEffect
{
    public int TypeCode { get; }

    public AvsUnknownEffect(int typeCode)
    {
        TypeCode = typeCode;
        IsEnabled = false; // do not run
    }

    /// <inheritdoc/>
    public override string DisplayName => $"Unknown effect (typecode 0x{TypeCode:X2})";

    /// <inheritdoc/>
    public override void Render(RgbaFrame target, AvsRenderContext context, in VisualizationInputs inputs)
    {
        // No-op — keeps the chain rolling.
    }
}
