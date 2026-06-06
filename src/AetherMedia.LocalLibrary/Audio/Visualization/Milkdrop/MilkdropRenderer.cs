// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization.Milkdrop;

/// <summary>
/// V1 CPU Milkdrop renderer. Each frame:
/// <list type="number">
///   <item><description>Splits the incoming spectrum into bass / mid / treb energies and runs the preset's per_frame equation block via <see cref="MilkdropEvaluator"/>.</description></item>
///   <item><description>Applies the preset's decay factor to the previous frame buffer.</description></item>
///   <item><description>Re-samples the previous frame with the per-frame zoom + rotation around (cx, cy) — a bilinear "warp" that captures the structural feel of Milkdrop without running the per-pixel mesh.</description></item>
///   <item><description>Overlays the waveform line in the configured colour with optional additive blending.</description></item>
/// </list>
///
/// <para>
/// Custom shapes, custom waves, per_pixel warp mesh, motion vectors, and
/// HLSL warp / comp shaders are deferred to v2 — those need either an
/// equation-per-vertex loop or a real GPU path. The shipped output is
/// recognisable Milkdrop motion: presets that rely only on per_frame
/// dynamics (the majority of "preset_basic" .milk files) render correctly.
/// </para>
/// </summary>
public sealed class MilkdropRenderer : IVisualizationRenderer
{
    private readonly MilkdropPreset _preset;
    private readonly MilkdropEvaluator _evaluator;

    private byte[]? _previous;
    private int _previousWidth;
    private int _previousHeight;
    private long _frameIndex;
    private double _elapsedSeconds;

    /// <summary>Construct a renderer bound to a preset.</summary>
    public MilkdropRenderer(MilkdropPreset preset)
    {
        _preset = preset ?? throw new ArgumentNullException(nameof(preset));
        _evaluator = new MilkdropEvaluator(preset);
    }

    /// <inheritdoc/>
    public string DisplayName => $"Milkdrop: {_preset.SectionName}";

    /// <summary>Live evaluator state — exposed for tests and HUDs.</summary>
    public MilkdropEvaluator Evaluator => _evaluator;

    /// <inheritdoc/>
    public void Render(in VisualizationInputs inputs, RgbaFrame target)
    {
        ArgumentNullException.ThrowIfNull(target);
        EnsureBackBuffer(target);

        var (bass, mid, treb) = ComputeBands(inputs.Spectrum);
        var fps = 60.0; // Renderer caller doesn't pass real fps; 60 is the Milkdrop default assumption.
        _frameIndex++;
        _elapsedSeconds += 1.0 / fps;

        _evaluator.EvaluateFrame(_elapsedSeconds, _frameIndex, fps, bass, mid, treb);
        var state = _evaluator.State;

        // 1. Decay the previous frame.
        var decay = (float)state.Decay;
        for (var i = 0; i < _previous!.Length; i += 4)
        {
            _previous[i]     = (byte)(_previous[i]     * decay);
            _previous[i + 1] = (byte)(_previous[i + 1] * decay);
            _previous[i + 2] = (byte)(_previous[i + 2] * decay);
            // alpha stays opaque
        }

        // 2. Warp: sample the decayed previous frame back to the target,
        // applying per-frame zoom + rotation around (cx, cy).
        ApplyWarp(target, state);

        // 3. Waveform overlay.
        DrawWaveform(target, inputs, state);

        // 4. Carry the new frame forward as the next "previous".
        Buffer.BlockCopy(target.Pixels, 0, _previous, 0, _previous.Length);
        _previousWidth = target.Width;
        _previousHeight = target.Height;
    }

    private void EnsureBackBuffer(RgbaFrame target)
    {
        if (_previous is null || _previousWidth != target.Width || _previousHeight != target.Height)
        {
            _previous = new byte[target.Pixels.Length];
            _previousWidth = target.Width;
            _previousHeight = target.Height;
            // First frame: start from black.
            for (var i = 3; i < _previous.Length; i += 4) _previous[i] = 0xFF;
        }
    }

    /// <summary>
    /// Split the incoming spectrum magnitudes into 3 logarithmic bands and
    /// normalise to Milkdrop's nominal 0..~3 range.
    /// </summary>
    private static (double Bass, double Mid, double Treb) ComputeBands(SpectrumFrame? spectrum)
    {
        if (spectrum is not { Magnitudes.Length: > 0 } sp) return (0, 0, 0);
        var n = sp.Magnitudes.Length;
        // Split: 0..1/8 = bass, 1/8..1/2 = mid, 1/2..end = treb.
        var b1 = Math.Max(1, n / 8);
        var b2 = n / 2;
        double bSum = 0, mSum = 0, tSum = 0;
        for (var i = 0; i < b1; i++) bSum += sp.Magnitudes[i];
        for (var i = b1; i < b2; i++) mSum += sp.Magnitudes[i];
        for (var i = b2; i < n; i++) tSum += sp.Magnitudes[i];
        // Average per bucket, scaled so a fully-saturated band reads ~1.
        var bass = (bSum / b1) * 3.0;
        var mid  = (mSum / Math.Max(1, b2 - b1)) * 3.0;
        var treb = (tSum / Math.Max(1, n - b2)) * 3.0;
        return (bass, mid, treb);
    }

    /// <summary>
    /// Bilinear-sample the previous frame back into the target at the
    /// per-frame zoom + rotation around (cx, cy). Pixels off the previous
    /// frame are filled black.
    /// </summary>
    private void ApplyWarp(RgbaFrame target, MilkdropFrameState state)
    {
        var w = target.Width;
        var h = target.Height;
        var prev = _previous!;
        var cx = state.Cx * w;
        var cy = state.Cy * h;
        var zoom = state.Zoom <= 0.001 ? 0.001 : state.Zoom;
        var cos = (float)Math.Cos(-state.Rot);
        var sin = (float)Math.Sin(-state.Rot);
        var dx = state.Dx * w;
        var dy = state.Dy * h;

        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                // Source coords = inverse(zoom + rotate + translate) applied to (x,y).
                var rx = (x - cx) / zoom;
                var ry = (y - cy) / zoom;
                var sx = (float)(rx * cos - ry * sin + cx - dx);
                var sy = (float)(rx * sin + ry * cos + cy - dy);

                if (sx < 0 || sx >= w - 1 || sy < 0 || sy >= h - 1)
                {
                    var ti = (y * w + x) * 4;
                    target.Pixels[ti]     = 0;
                    target.Pixels[ti + 1] = 0;
                    target.Pixels[ti + 2] = 0;
                    target.Pixels[ti + 3] = 0xFF;
                    continue;
                }

                var x0 = (int)sx; var y0 = (int)sy;
                var fx = sx - x0; var fy = sy - y0;
                var i00 = (y0 * w + x0) * 4;
                var i10 = i00 + 4;
                var i01 = i00 + w * 4;
                var i11 = i01 + 4;

                var r = Blend(prev[i00], prev[i10], prev[i01], prev[i11], fx, fy);
                var g = Blend(prev[i00 + 1], prev[i10 + 1], prev[i01 + 1], prev[i11 + 1], fx, fy);
                var b = Blend(prev[i00 + 2], prev[i10 + 2], prev[i01 + 2], prev[i11 + 2], fx, fy);

                var ti2 = (y * w + x) * 4;
                target.Pixels[ti2]     = r;
                target.Pixels[ti2 + 1] = g;
                target.Pixels[ti2 + 2] = b;
                target.Pixels[ti2 + 3] = 0xFF;
            }
        }
    }

    private static byte Blend(byte a, byte b, byte c, byte d, float fx, float fy)
    {
        var top = a * (1 - fx) + b * fx;
        var bot = c * (1 - fx) + d * fx;
        var v = top * (1 - fy) + bot * fy;
        return (byte)Math.Clamp(v, 0, 255);
    }

    /// <summary>
    /// Draw the time-domain waveform centred at (wave_x, wave_y) at the
    /// configured colour. Additive blending lights the existing frame.
    /// </summary>
    private void DrawWaveform(RgbaFrame target, in VisualizationInputs inputs, MilkdropFrameState state)
    {
        var samples = inputs.TimeDomainSamples.Span;
        if (samples.Length == 0) return;

        var w = target.Width;
        var h = target.Height;
        var p = _evaluator.Parameters;

        var alpha = (float)Math.Clamp(p.WaveAlpha, 0.0, 1.0);
        var scale = (float)p.WaveScale * h * 0.25f;
        var cx = (float)(p.WaveX * w);
        var cy = (float)(p.WaveY * h);
        var maxX = w - 1;

        var r = (byte)Math.Clamp(p.WaveR * 255.0, 0, 255);
        var g = (byte)Math.Clamp(p.WaveG * 255.0, 0, 255);
        var b = (byte)Math.Clamp(p.WaveB * 255.0, 0, 255);

        var channels = Math.Max(1, inputs.Channels);
        var monoLen = samples.Length / channels;
        if (monoLen <= 0) return;

        // Spread the waveform horizontally around cx by ±w/3, like Milkdrop's
        // default WaveMode=0 (centre line).
        var halfW = w / 3;
        var xStart = (int)cx - halfW;
        var xEnd   = (int)cx + halfW;
        if (xStart < 0) xStart = 0;
        if (xEnd > maxX) xEnd = maxX;
        var span = Math.Max(1, xEnd - xStart);

        var prevY = (int)cy;
        for (var x = xStart; x <= xEnd; x++)
        {
            var srcIdx = (int)((long)(x - xStart) * monoLen / span) * channels;
            if (srcIdx >= samples.Length) break;
            var s = samples[srcIdx];
            if (channels > 1 && srcIdx + 1 < samples.Length) s = (s + samples[srcIdx + 1]) * 0.5f;
            var y = (int)(cy - s * scale);
            if (y < 0) y = 0;
            if (y >= h) y = h - 1;

            var y0 = Math.Min(prevY, y);
            var y1 = Math.Max(prevY, y);
            for (var py = y0; py <= y1; py++)
                Composite(target, x, py, r, g, b, alpha, additive: p.AdditiveWaves);
            prevY = y;
        }
    }

    private static void Composite(RgbaFrame target, int x, int y, byte r, byte g, byte b, float alpha, bool additive)
    {
        var i = (y * target.Width + x) * 4;
        if (additive)
        {
            target.Pixels[i]     = (byte)Math.Min(255, target.Pixels[i]     + r * alpha);
            target.Pixels[i + 1] = (byte)Math.Min(255, target.Pixels[i + 1] + g * alpha);
            target.Pixels[i + 2] = (byte)Math.Min(255, target.Pixels[i + 2] + b * alpha);
        }
        else
        {
            target.Pixels[i]     = (byte)(target.Pixels[i]     * (1 - alpha) + r * alpha);
            target.Pixels[i + 1] = (byte)(target.Pixels[i + 1] * (1 - alpha) + g * alpha);
            target.Pixels[i + 2] = (byte)(target.Pixels[i + 2] * (1 - alpha) + b * alpha);
        }
        target.Pixels[i + 3] = 0xFF;
    }
}
