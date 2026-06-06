// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization.Avs;

/// <summary>
/// Executes a parsed <see cref="AvsPreset"/>'s effect chain against an
/// <see cref="RgbaFrame"/>. Each frame:
/// <list type="number">
///   <item><description>If <see cref="AvsPreset.ClearEveryFrame"/> is set, clears the target to black.</description></item>
///   <item><description>Updates the shared <see cref="AvsRenderContext"/> (time, frame index, bass/mid/treb, on-beat flag).</description></item>
///   <item><description>Runs every <see cref="AvsEffect"/> in the chain in document order, applying each to the same target frame.</description></item>
/// </list>
///
/// <para>
/// Concrete effect runtimes live under <c>Audio/Visualization/Avs/Effects/</c>.
/// Type codes not yet represented fall through to <see cref="AvsUnknownEffect"/>
/// (no-op) so a preset using an unsupported effect still renders the rest
/// of its chain cleanly. Adding a new effect = one new file + one
/// dispatch arm in <see cref="AvsEffectFactory.Create"/>.
/// </para>
/// </summary>
public sealed class AvsRenderer : IVisualizationRenderer
{
    private readonly AvsPreset _preset;
    private readonly AvsEffectChain _chain;
    private AvsRenderContext? _context;
    private int _ctxWidth, _ctxHeight;
    private long _frameIndex;
    private double _elapsed;

    public AvsRenderer(AvsPreset preset)
    {
        _preset = preset ?? throw new ArgumentNullException(nameof(preset));
        _chain  = AvsEffectChain.FromPreset(preset);
    }

    /// <inheritdoc/>
    public string DisplayName =>
        $"AVS chain ({_chain.Effects.Count} effects, {_chain.Effects.Count(e => e is AvsUnknownEffect)} unknown)";

    /// <summary>The compiled chain, exposed for diagnostics.</summary>
    public AvsEffectChain Chain => _chain;

    /// <inheritdoc/>
    public void Render(in VisualizationInputs inputs, RgbaFrame target)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (_preset.ClearEveryFrame)
            target.Clear(0, 0, 0, 0xFF);

        if (_context is null || _ctxWidth != target.Width || _ctxHeight != target.Height)
        {
            _context = new AvsRenderContext(target.Width, target.Height);
            _ctxWidth = target.Width;
            _ctxHeight = target.Height;
        }

        _frameIndex++;
        _elapsed += 1.0 / 60.0;
        _context.TimeSeconds = _elapsed;
        _context.FrameIndex = _frameIndex;
        var (bass, mid, treb) = ComputeBands(inputs.Spectrum);
        _context.Bass = bass;
        _context.Mid = mid;
        _context.Treb = treb;
        // Simple onset detection: bass crosses a threshold (every preset's
        // own NS-EEL scripts re-implement smarter detection if needed).
        _context.OnBeat = bass > 1.5;

        _chain.Render(target, _context, in inputs);
    }

    private static (double Bass, double Mid, double Treb) ComputeBands(SpectrumFrame? sp)
    {
        if (sp is not { Magnitudes.Length: > 0 } s) return (0, 0, 0);
        var n = s.Magnitudes.Length;
        var b1 = Math.Max(1, n / 8);
        var b2 = n / 2;
        double bSum = 0, mSum = 0, tSum = 0;
        for (var i = 0; i < b1; i++) bSum += s.Magnitudes[i];
        for (var i = b1; i < b2; i++) mSum += s.Magnitudes[i];
        for (var i = b2; i < n;  i++) tSum += s.Magnitudes[i];
        return (bSum / b1 * 3.0, mSum / Math.Max(1, b2 - b1) * 3.0, tSum / Math.Max(1, n - b2) * 3.0);
    }
}
