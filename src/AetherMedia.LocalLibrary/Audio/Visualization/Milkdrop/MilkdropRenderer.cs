// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization.Milkdrop;

/// <summary>
/// Full Milkdrop preset renderer. Each frame:
/// <list type="number">
///   <item><description>Splits the spectrum into bass / mid / treb and runs the preset's per_frame equation block.</description></item>
///   <item><description>Re-evaluates per_pixel equations at every <see cref="MilkdropWarpMesh"/> vertex (32×24 by default) — each vertex caches its source UV in the previous frame.</description></item>
///   <item><description>Decays the previous frame by the per-frame <c>decay</c> factor, then rasterises the warped output by bilinear-interpolating the mesh source UVs across each quad and sampling the previous frame.</description></item>
///   <item><description>Runs every enabled custom shape (1..4) × num_inst instances; each instance is rendered as an N-sided polygon at (x, y) with the per-instance fill + border colours.</description></item>
///   <item><description>Runs every enabled custom wave (1..4); each draws a polyline whose vertex positions / colours come from per_point equations.</description></item>
///   <item><description>Draws the default waveform overlay (when no custom wave provides one) in the configured <c>wave_r/g/b</c> colour.</description></item>
///   <item><description>If the preset carries a warp / composite HLSL shader, runs it per-pixel via <see cref="MilkdropShader"/>.</description></item>
/// </list>
/// </summary>
public sealed class MilkdropRenderer : IVisualizationRenderer
{
    private readonly MilkdropPreset _preset;
    private readonly MilkdropEvaluator _evaluator;
    private readonly MilkdropWarpMesh _mesh;
    private readonly List<MilkdropShapeEvaluator> _shapeEvaluators = new();
    private readonly List<MilkdropWaveEvaluator> _waveEvaluators = new();
    private readonly MilkdropShader? _warpShader;
    private readonly MilkdropShader? _compShader;

    private byte[]? _previous;
    private int _w, _h;
    private long _frameIndex;
    private double _elapsedSeconds;

    public MilkdropRenderer(MilkdropPreset preset)
    {
        _preset = preset ?? throw new ArgumentNullException(nameof(preset));
        _evaluator = new MilkdropEvaluator(preset);
        _mesh = new MilkdropWarpMesh();
        foreach (var s in preset.Shapes)
            if (s.Enabled) _shapeEvaluators.Add(new MilkdropShapeEvaluator(s));
        foreach (var w in preset.Waves)
            if (w.Enabled) _waveEvaluators.Add(new MilkdropWaveEvaluator(w));

        if (!string.IsNullOrWhiteSpace(preset.WarpShader))
            _warpShader = MilkdropShader.TryCompile(preset.WarpShader);
        if (!string.IsNullOrWhiteSpace(preset.CompositeShader))
            _compShader = MilkdropShader.TryCompile(preset.CompositeShader);
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
        var fps = 60.0;
        _frameIndex++;
        _elapsedSeconds += 1.0 / fps;

        _evaluator.EvaluateFrame(_elapsedSeconds, _frameIndex, fps, bass, mid, treb);
        _mesh.Compute(_evaluator);
        var state = _evaluator.State;

        // 1. Decay the previous frame in-place.
        var decay = (float)state.Decay;
        for (var i = 0; i < _previous!.Length; i += 4)
        {
            _previous[i]     = (byte)(_previous[i]     * decay);
            _previous[i + 1] = (byte)(_previous[i + 1] * decay);
            _previous[i + 2] = (byte)(_previous[i + 2] * decay);
        }

        // 2. Warp: bilinear-sample previous frame through the mesh UVs.
        WarpThroughMesh(target);

        // 3. Optional warp HLSL shader: post-process by running per-pixel.
        _warpShader?.RenderPerPixel(target, _previous!, _evaluator);

        // 4. Custom shapes.
        foreach (var sev in _shapeEvaluators)
            RenderShape(target, sev, bass, mid, treb);

        // 5. Custom waves.
        var anyCustomWave = false;
        var qSpan = SnapshotQ();
        foreach (var wev in _waveEvaluators)
        {
            anyCustomWave = true;
            RenderWave(target, wev, qSpan, inputs);
        }

        // 6. Default waveform if no custom wave provided one.
        if (!anyCustomWave) DrawDefaultWaveform(target, inputs);

        // 7. Optional composite HLSL shader.
        _compShader?.RenderPerPixel(target, _previous!, _evaluator);

        Buffer.BlockCopy(target.Pixels, 0, _previous!, 0, _previous!.Length);
        _w = target.Width;
        _h = target.Height;
    }

    /// <summary>Snapshot of the per-frame q registers, for shape / wave evaluators.</summary>
    private double[] SnapshotQ()
    {
        var q = new double[32];
        for (var i = 0; i < 32; i++) q[i] = _evaluator.GetQ(i + 1);
        return q;
    }

    private void EnsureBackBuffer(RgbaFrame target)
    {
        if (_previous is null || _w != target.Width || _h != target.Height)
        {
            _previous = new byte[target.Pixels.Length];
            _w = target.Width;
            _h = target.Height;
            for (var i = 3; i < _previous.Length; i += 4) _previous[i] = 0xFF;
        }
    }

    private static (double Bass, double Mid, double Treb) ComputeBands(SpectrumFrame? spectrum)
    {
        if (spectrum is not { Magnitudes.Length: > 0 } sp) return (0, 0, 0);
        var n = sp.Magnitudes.Length;
        var b1 = Math.Max(1, n / 8);
        var b2 = n / 2;
        double bSum = 0, mSum = 0, tSum = 0;
        for (var i = 0; i < b1; i++) bSum += sp.Magnitudes[i];
        for (var i = b1; i < b2; i++) mSum += sp.Magnitudes[i];
        for (var i = b2; i < n; i++) tSum += sp.Magnitudes[i];
        return ((bSum / b1) * 3.0, (mSum / Math.Max(1, b2 - b1)) * 3.0, (tSum / Math.Max(1, n - b2)) * 3.0);
    }

    /// <summary>
    /// Rasterise the mesh: for every output pixel, locate its containing
    /// quad, bilinear-interpolate the four corner source UVs, and bilinear-
    /// sample the previous frame at the result.
    /// </summary>
    private void WarpThroughMesh(RgbaFrame target)
    {
        var w = target.Width;
        var h = target.Height;
        var mw = _mesh.Width;
        var mh = _mesh.Height;
        var prev = _previous!;

        for (var y = 0; y < h; y++)
        {
            var gy = (double)y / (h - 1);
            var gyScaled = gy * (mh - 1);
            var iy = (int)gyScaled;
            if (iy >= mh - 1) iy = mh - 2;
            var fy = gyScaled - iy;

            for (var x = 0; x < w; x++)
            {
                var gx = (double)x / (w - 1);
                var gxScaled = gx * (mw - 1);
                var ix = (int)gxScaled;
                if (ix >= mw - 1) ix = mw - 2;
                var fx = gxScaled - ix;

                var v00 = _mesh[ix,     iy    ];
                var v10 = _mesh[ix + 1, iy    ];
                var v01 = _mesh[ix,     iy + 1];
                var v11 = _mesh[ix + 1, iy + 1];

                var u = (v00.SourceU * (1 - fx) + v10.SourceU * fx) * (1 - fy)
                      + (v01.SourceU * (1 - fx) + v11.SourceU * fx) * fy;
                var v = (v00.SourceV * (1 - fx) + v10.SourceV * fx) * (1 - fy)
                      + (v01.SourceV * (1 - fx) + v11.SourceV * fx) * fy;

                var sx = u * (w - 1);
                var sy = v * (h - 1);

                int ti = (y * w + x) * 4;
                if (sx < 0 || sx >= w - 1 || sy < 0 || sy >= h - 1)
                {
                    target.Pixels[ti] = 0;
                    target.Pixels[ti + 1] = 0;
                    target.Pixels[ti + 2] = 0;
                    target.Pixels[ti + 3] = 0xFF;
                    continue;
                }

                var x0 = (int)sx; var y0 = (int)sy;
                var ffx = (float)(sx - x0); var ffy = (float)(sy - y0);
                var i00 = (y0 * w + x0) * 4;
                var i10 = i00 + 4;
                var i01 = i00 + w * 4;
                var i11 = i01 + 4;

                target.Pixels[ti]     = Lerp4(prev[i00], prev[i10], prev[i01], prev[i11], ffx, ffy);
                target.Pixels[ti + 1] = Lerp4(prev[i00 + 1], prev[i10 + 1], prev[i01 + 1], prev[i11 + 1], ffx, ffy);
                target.Pixels[ti + 2] = Lerp4(prev[i00 + 2], prev[i10 + 2], prev[i01 + 2], prev[i11 + 2], ffx, ffy);
                target.Pixels[ti + 3] = 0xFF;
            }
        }
    }

    private static byte Lerp4(byte a, byte b, byte c, byte d, float fx, float fy)
    {
        var top = a * (1 - fx) + b * fx;
        var bot = c * (1 - fx) + d * fx;
        return (byte)Math.Clamp(top * (1 - fy) + bot * fy, 0, 255);
    }

    private void RenderShape(RgbaFrame target, MilkdropShapeEvaluator sev, double bass, double mid, double treb)
    {
        var qSpan = SnapshotQ();
        var n = sev.Shape.Instances;
        for (var inst = 0; inst < n; inst++)
        {
            var s = sev.Evaluate(inst, _elapsedSeconds, _frameIndex, 60.0, bass, mid, treb, qSpan);
            DrawShapeInstance(target, s);
        }
    }

    /// <summary>Rasterise an N-sided polygon at (X, Y) with the configured colours.</summary>
    private void DrawShapeInstance(RgbaFrame target, MilkdropShapeInstance shape)
    {
        var w = target.Width;
        var h = target.Height;
        var cx = (float)(shape.X * w);
        var cy = (float)(shape.Y * h);
        var radius = (float)(shape.Radius * Math.Min(w, h));
        if (radius <= 0) return;
        var sides = Math.Max(3, shape.Sides);

        // Compute vertices.
        Span<float> vx = stackalloc float[sides];
        Span<float> vy = stackalloc float[sides];
        for (var i = 0; i < sides; i++)
        {
            var ang = shape.Angle + 2.0 * Math.PI * i / sides;
            vx[i] = cx + radius * (float)Math.Cos(ang);
            vy[i] = cy + radius * (float)Math.Sin(ang);
        }

        // Triangle-fan rasterise — fill colour blends towards CenterRgba at centre.
        for (var i = 0; i < sides; i++)
        {
            var j = (i + 1) % sides;
            FillTriangle(target, cx, cy, vx[i], vy[i], vx[j], vy[j],
                shape.CenterRgba, shape.FillRgba, shape.FillRgba, shape.Additive);
        }
    }

    private static void FillTriangle(RgbaFrame f,
        float ax, float ay, float bx, float by, float cx, float cy,
        (float R, float G, float B, float A) ca,
        (float R, float G, float B, float A) cb,
        (float R, float G, float B, float A) cc,
        bool additive)
    {
        var w = f.Width;
        var h = f.Height;
        var minX = Math.Max(0, (int)Math.Floor(Math.Min(ax, Math.Min(bx, cx))));
        var maxX = Math.Min(w - 1, (int)Math.Ceiling(Math.Max(ax, Math.Max(bx, cx))));
        var minY = Math.Max(0, (int)Math.Floor(Math.Min(ay, Math.Min(by, cy))));
        var maxY = Math.Min(h - 1, (int)Math.Ceiling(Math.Max(ay, Math.Max(by, cy))));

        var denom = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy);
        if (Math.Abs(denom) < 1e-5f) return;

        for (var y = minY; y <= maxY; y++)
        for (var x = minX; x <= maxX; x++)
        {
            var px = x + 0.5f;
            var py = y + 0.5f;
            var l1 = ((by - cy) * (px - cx) + (cx - bx) * (py - cy)) / denom;
            var l2 = ((cy - ay) * (px - cx) + (ax - cx) * (py - cy)) / denom;
            var l3 = 1.0f - l1 - l2;
            if (l1 < 0 || l2 < 0 || l3 < 0) continue;

            var r = l1 * ca.R + l2 * cb.R + l3 * cc.R;
            var g = l1 * ca.G + l2 * cb.G + l3 * cc.G;
            var b = l1 * ca.B + l2 * cb.B + l3 * cc.B;
            var a = l1 * ca.A + l2 * cb.A + l3 * cc.A;
            Composite(f, x, y, (byte)Math.Clamp(r * 255, 0, 255), (byte)Math.Clamp(g * 255, 0, 255), (byte)Math.Clamp(b * 255, 0, 255), a, additive);
        }
    }

    private void RenderWave(RgbaFrame target, MilkdropWaveEvaluator wev, ReadOnlySpan<double> qSpan, in VisualizationInputs inputs)
    {
        var samples = inputs.TimeDomainSamples.Span;
        var channels = Math.Max(1, inputs.Channels);
        var monoLen = samples.Length / channels;
        if (monoLen <= 0) return;
        // Split into L/R if available; mono otherwise.
        var l = new float[monoLen];
        var r = new float[monoLen];
        for (var i = 0; i < monoLen; i++)
        {
            l[i] = samples[i * channels];
            r[i] = channels > 1 ? samples[i * channels + 1] : l[i];
        }

        var (bass, mid, treb) = ComputeBands(inputs.Spectrum);
        var points = wev.EvaluateFrame(_elapsedSeconds, _frameIndex, 60.0, bass, mid, treb, qSpan, l, r);

        var w = target.Width;
        var h = target.Height;
        var additive = wev.Wave.Additive;
        for (var i = 1; i < points.Count; i++)
        {
            var p0 = points[i - 1];
            var p1 = points[i];
            var x0 = (int)(p0.X * w); var y0 = (int)(p0.Y * h);
            var x1 = (int)(p1.X * w); var y1 = (int)(p1.Y * h);
            DrawLineRgba(target, x0, y0, x1, y1, p1.Rgba, additive);
        }
    }

    private static void DrawLineRgba(RgbaFrame f, int x0, int y0, int x1, int y1, (float R, float G, float B, float A) rgba, bool additive)
    {
        // Bresenham.
        var dx = Math.Abs(x1 - x0);
        var dy = Math.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var err = dx - dy;
        var r = (byte)Math.Clamp(rgba.R * 255, 0, 255);
        var g = (byte)Math.Clamp(rgba.G * 255, 0, 255);
        var b = (byte)Math.Clamp(rgba.B * 255, 0, 255);
        while (true)
        {
            if (x0 >= 0 && x0 < f.Width && y0 >= 0 && y0 < f.Height)
                Composite(f, x0, y0, r, g, b, rgba.A, additive);
            if (x0 == x1 && y0 == y1) break;
            var e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
    }

    private void DrawDefaultWaveform(RgbaFrame target, in VisualizationInputs inputs)
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

        var r = (byte)Math.Clamp(p.WaveR * 255.0, 0, 255);
        var g = (byte)Math.Clamp(p.WaveG * 255.0, 0, 255);
        var b = (byte)Math.Clamp(p.WaveB * 255.0, 0, 255);

        var channels = Math.Max(1, inputs.Channels);
        var monoLen = samples.Length / channels;
        if (monoLen <= 0) return;

        var halfW = w / 3;
        var xStart = (int)cx - halfW;
        var xEnd   = (int)cx + halfW;
        if (xStart < 0) xStart = 0;
        if (xEnd > w - 1) xEnd = w - 1;
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
        if (alpha <= 0) return;
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
