// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization;

/// <summary>
/// Classic Winamp segmented-bar spectrum — discrete 4-pixel-tall blocks
/// stacked vertically with a single-pixel gap between bars. Optionally takes
/// the <c>viscolor.txt</c> palette loaded by <c>WinampSkinLoader</c> so the
/// look matches whichever skin is active.
/// </summary>
public sealed class ClassicWinampBarsRenderer : IVisualizationRenderer
{
    private const int BlockHeight = 3;
    private const int BlockGap    = 1;

    /// <summary>Number of bars.</summary>
    public int BarCount { get; init; } = 19;

    /// <summary>
    /// Optional skin palette. Winamp viscolor.txt is 24 entries: 0 = background,
    /// 1 = grid, 2..17 = analyzer block colours (low to high), 18..23 = peak
    /// dot + oscilloscope colours. Null falls back to a built-in palette.
    /// </summary>
    public IReadOnlyList<(byte R, byte G, byte B)>? Palette { get; init; }

    /// <summary>Floor magnitude in dB at which a bar shows as empty.</summary>
    public double FloorDb { get; init; } = -60.0;

    /// <inheritdoc/>
    public string DisplayName => "Classic Winamp Bars";

    /// <inheritdoc/>
    public void Render(in VisualizationInputs inputs, RgbaFrame target)
    {
        ArgumentNullException.ThrowIfNull(target);

        var palette = Palette is { Count: >= 18 } ? Palette : DefaultPalette;
        var bg = palette[0];
        target.Clear(bg.R, bg.G, bg.B, 0xFF);

        if (inputs.Spectrum is not { } spectrum) return;
        if (spectrum.Magnitudes.Length == 0) return;

        var w = target.Width;
        var h = target.Height;
        var bars = Math.Min(BarCount, w);
        var barW = Math.Max(1, w / bars - 1);
        var unit = BlockHeight + BlockGap;
        var blocksPerBar = Math.Max(1, h / unit);

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
            var blocks = (int)(norm * blocksPerBar);

            var xBase = b * (barW + 1);
            for (var n = 0; n < blocks; n++)
            {
                // Block colour: low blocks use palette indices 2..17 from low to high.
                var colorIdx = 2 + Math.Min(15, n * 16 / Math.Max(1, blocksPerBar));
                if (colorIdx >= palette.Count) colorIdx = palette.Count - 1;
                var (r, g, bl) = palette[colorIdx];

                var yTop = h - (n + 1) * unit;
                for (var dy = 0; dy < BlockHeight; dy++)
                {
                    var y = yTop + dy;
                    if (y < 0 || y >= h) continue;
                    for (var dx = 0; dx < barW; dx++)
                    {
                        var x = xBase + dx;
                        if (x < 0 || x >= w) continue;
                        target.SetPixel(x, y, r, g, bl, 0xFF);
                    }
                }
            }
        }
    }

    /// <summary>Reasonable default analyzer palette in the Winamp colour vocabulary.</summary>
    public static IReadOnlyList<(byte R, byte G, byte B)> DefaultPalette { get; } =
    [
        (0, 0, 0),        // 0 bg
        (24, 33, 41),     // 1 grid
        (24, 132,  16),   // 2 low
        (40, 148,  24),
        (56, 165,  32),
        (72, 181,  40),
        (96, 198,  56),
        (120,222,  64),
        (160,236,  72),
        (200,242,  80),
        (220,240,  64),
        (245,222,  48),
        (250,180,  32),
        (250,140,  24),
        (250, 96,  16),
        (240, 56,   8),
        (224, 24,   8),
        (208,  0,   0),   // 17 high
        (255,255,255),    // 18 peak
        ( 24,140,  16),   // 19..23 — oscilloscope range
        ( 40,160,  24),
        ( 64,200,  40),
        (140,240,  72),
        (255,255,255),
    ];
}
