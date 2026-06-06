// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization.Avs;

/// <summary>
/// Ordered set of <see cref="AvsEffect"/>s. <see cref="Render"/> iterates
/// them in document order, applying each in turn to the same target frame
/// and shared <see cref="AvsRenderContext"/>.
/// </summary>
public sealed class AvsEffectChain
{
    private readonly List<AvsEffect> _effects;

    public AvsEffectChain(IEnumerable<AvsEffect> effects)
    {
        _effects = (effects ?? throw new ArgumentNullException(nameof(effects))).ToList();
    }

    /// <summary>Build a chain from a parsed <see cref="AvsPreset"/>.</summary>
    public static AvsEffectChain FromPreset(AvsPreset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);
        var list = new List<AvsEffect>(preset.EffectBlobs.Count);
        foreach (var blob in preset.EffectBlobs)
            list.Add(AvsEffectFactory.Create(blob.TypeCode, blob.Payload));
        return new AvsEffectChain(list);
    }

    public IReadOnlyList<AvsEffect> Effects => _effects;

    /// <summary>Run the chain.</summary>
    public void Render(RgbaFrame target, AvsRenderContext context, in VisualizationInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(context);
        foreach (var fx in _effects)
            fx.Render(target, context, in inputs);
    }
}
