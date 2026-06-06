// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization;

/// <summary>
/// "Geiss"-style algorithmic visualizer — Ryan Geiss's signature look from
/// the original 1998 Winamp plugin. Pure pattern, no presets: a slowly
/// rotating warp anchored to the centre, decayed feedback, bass-reactive
/// zoom, and a hue-rotating colour palette overlaid through a smoothed
/// spectrum gradient. Recognisable Geiss aesthetic in ~150 lines of pure
/// managed code.
/// </summary>
public sealed class GeissRenderer : IVisualizationRenderer
{
    private byte[]? _previous;
    private int _w, _h;
    private double _rot;
    private double _zoom = 1.0;
    private double _hue;

    /// <inheritdoc/>
    public string DisplayName => "Geiss";

    /// <inheritdoc/>
    public void Render(in VisualizationInputs inputs, RgbaFrame target)
    {
        ArgumentNullException.ThrowIfNull(target);
        EnsureBack(target);

        var (bass, mid, treb) = SplitBands(inputs.Spectrum);

        // Rotation and zoom are bass + treble driven.
        _rot += 0.005 + 0.04 * treb;
        _zoom += 0.02 * (1.0 + 2.0 * bass - _zoom);
        _hue = (_hue + 0.004 + 0.02 * mid) % 1.0;

        // Sample the decayed previous frame through a zoom + rotate around centre.
        Warp(target, _zoom, _rot, decay: 0.93f);

        // Splash the spectrum across the frame in the current hue.
        SplashSpectrum(target, inputs.Spectrum, _hue);

        Buffer.BlockCopy(target.Pixels, 0, _previous!, 0, _previous!.Length);
    }

    private void EnsureBack(RgbaFrame target)
    {
        if (_previous is null || _w != target.Width || _h != target.Height)
        {
            _previous = new byte[target.Pixels.Length];
            _w = target.Width;
            _h = target.Height;
            for (var i = 3; i < _previous.Length; i += 4) _previous[i] = 0xFF;
        }
    }

    private static (double Bass, double Mid, double Treb) SplitBands(SpectrumFrame? sp)
    {
        if (sp is not { Magnitudes.Length: > 0 } s) return (0, 0, 0);
        var n = s.Magnitudes.Length;
        var b1 = Math.Max(1, n / 8);
        var b2 = n / 2;
        double bass = 0, mid = 0, treb = 0;
        for (var i = 0; i < b1; i++) bass += s.Magnitudes[i];
        for (var i = b1; i < b2; i++) mid  += s.Magnitudes[i];
        for (var i = b2; i < n;  i++) treb += s.Magnitudes[i];
        return (bass / b1, mid / Math.Max(1, b2 - b1), treb / Math.Max(1, n - b2));
    }

    private void Warp(RgbaFrame target, double zoom, double rot, float decay)
    {
        var w = target.Width;
        var h = target.Height;
        var cx = w / 2.0;
        var cy = h / 2.0;
        var cos = (float)Math.Cos(-rot);
        var sin = (float)Math.Sin(-rot);
        var zoomF = (float)Math.Max(0.001, zoom);
        var prev = _previous!;

        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var rx = (x - cx) / zoomF;
            var ry = (y - cy) / zoomF;
            var sx = (float)(rx * cos - ry * sin + cx);
            var sy = (float)(rx * sin + ry * cos + cy);

            if (sx < 0 || sx >= w - 1 || sy < 0 || sy >= h - 1)
            {
                var ti = (y * w + x) * 4;
                target.Pixels[ti]     = 0;
                target.Pixels[ti + 1] = 0;
                target.Pixels[ti + 2] = 0;
                target.Pixels[ti + 3] = 0xFF;
                continue;
            }

            var ix = (int)sx;
            var iy = (int)sy;
            var i = (iy * w + ix) * 4;
            var oi = (y * w + x) * 4;
            target.Pixels[oi]     = (byte)(prev[i]     * decay);
            target.Pixels[oi + 1] = (byte)(prev[i + 1] * decay);
            target.Pixels[oi + 2] = (byte)(prev[i + 2] * decay);
            target.Pixels[oi + 3] = 0xFF;
        }
    }

    private static void SplashSpectrum(RgbaFrame target, SpectrumFrame? sp, double hue)
    {
        if (sp is not { Magnitudes.Length: > 0 } s) return;
        var w = target.Width;
        var h = target.Height;
        var (r, g, b) = HsvToRgb(hue, 1.0, 1.0);

        for (var x = 0; x < w; x++)
        {
            var idx = (int)((long)x * s.Magnitudes.Length / w);
            if (idx >= s.Magnitudes.Length) break;
            var amp = Math.Min(1.0f, s.Magnitudes[idx] * 4f);
            if (amp < 0.05f) continue;
            var barH = (int)(amp * h);
            for (var y = h - barH; y < h; y++)
            {
                var i = (y * w + x) * 4;
                target.Pixels[i]     = (byte)Math.Min(255, target.Pixels[i]     + r * amp);
                target.Pixels[i + 1] = (byte)Math.Min(255, target.Pixels[i + 1] + g * amp);
                target.Pixels[i + 2] = (byte)Math.Min(255, target.Pixels[i + 2] + b * amp);
            }
        }
    }

    private static (byte R, byte G, byte B) HsvToRgb(double h, double s, double v)
    {
        h = (h - Math.Floor(h)) * 6.0;
        var i = (int)Math.Floor(h);
        var f = h - i;
        var p = v * (1.0 - s);
        var q = v * (1.0 - s * f);
        var t = v * (1.0 - s * (1.0 - f));
        double r, g, b;
        switch (i % 6)
        {
            case 0: r = v; g = t; b = p; break;
            case 1: r = q; g = v; b = p; break;
            case 2: r = p; g = v; b = t; break;
            case 3: r = p; g = q; b = v; break;
            case 4: r = t; g = p; b = v; break;
            default: r = v; g = p; b = q; break;
        }
        return ((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
    }
}
