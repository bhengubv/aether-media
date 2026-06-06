// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization;

/// <summary>
/// Smooth spectrum-bar renderer — log-frequency-grouped FFT magnitudes,
/// drawn as continuous filled bars from the bottom of the frame. The
/// "modern" spectrum analyzer style; for the chunky pixel-segmented Winamp
/// look use <see cref="ClassicWinampBarsRenderer"/>.
/// </summary>
public sealed class SpectrumBarsRenderer : IVisualizationRenderer
{
    /// <summary>Number of bars to render.</summary>
    public int BarCount { get; init; } = 32;

    /// <summary>Bar colour at full height (top).</summary>
    public (byte R, byte G, byte B, byte A) BarTop { get; init; } = (0x00, 0xCC, 0xFF, 0xFF);

    /// <summary>Bar colour at the base.</summary>
    public (byte R, byte G, byte B, byte A) BarBottom { get; init; } = (0x00, 0x33, 0x99, 0xFF);

    /// <summary>Background colour.</summary>
    public (byte R, byte G, byte B, byte A) Background { get; init; } = (0x00, 0x00, 0x00, 0xFF);

    /// <summary>Floor magnitude in dB at which a bar shows as empty.</summary>
    public double FloorDb { get; init; } = -60.0;

    /// <inheritdoc/>
    public string DisplayName => "Spectrum Bars";

    /// <inheritdoc/>
    public void Render(in VisualizationInputs inputs, RgbaFrame target)
    {
        ArgumentNullException.ThrowIfNull(target);
        target.Clear(Background.R, Background.G, Background.B, Background.A);

        if (inputs.Spectrum is not { } spectrum) return;
        if (spectrum.Magnitudes.Length == 0) return;

        var w = target.Width;
        var h = target.Height;
        var bars = Math.Min(BarCount, w);
        var barW = Math.Max(1, w / bars);

        // Log-band group FFT bins into BarCount groups: each bar = max of bins
        // in the group, converted to dB and mapped over FloorDb..0.
        var bins = spectrum.Magnitudes.Length;
        for (var b = 0; b < bars; b++)
        {
            var lo = (int)(bins * Math.Pow((double)b / bars, 2.0));
            var hi = (int)(bins * Math.Pow((double)(b + 1) / bars, 2.0));
            if (hi <= lo) hi = lo + 1;
            if (hi > bins) hi = bins;

            float peak = 0;
            for (var i = lo; i < hi; i++)
                if (spectrum.Magnitudes[i] > peak) peak = spectrum.Magnitudes[i];

            var db = peak > 0 ? 20.0 * Math.Log10(peak) : FloorDb;
            var norm = (db - FloorDb) / -FloorDb;
            if (norm < 0) norm = 0; if (norm > 1) norm = 1;

            var barH = (int)(norm * h);
            var x0 = b * barW;
            var x1 = Math.Min(w, x0 + barW - 1);
            for (var y = h - barH; y < h; y++)
            {
                var t = (double)(h - y) / Math.Max(1, barH); // 0 at base, 1 at top
                var (r, g, bl, a) = LerpColor(BarBottom, BarTop, t);
                for (var x = x0; x <= x1; x++)
                    target.SetPixel(x, y, r, g, bl, a);
            }
        }
    }

    private static (byte R, byte G, byte B, byte A) LerpColor(
        (byte R, byte G, byte B, byte A) a, (byte R, byte G, byte B, byte A) b, double t) =>
        ((byte)(a.R + (b.R - a.R) * t),
         (byte)(a.G + (b.G - a.G) * t),
         (byte)(a.B + (b.B - a.B) * t),
         (byte)(a.A + (b.A - a.A) * t));
}
