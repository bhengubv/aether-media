// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization;

/// <summary>
/// Classic waveform oscilloscope — plots the time-domain samples horizontally
/// across the frame, vertically centred, in a single colour. The Winamp
/// equivalent of "Oscilloscope" mode in the spectrum analyzer plugin.
/// </summary>
public sealed class OscilloscopeRenderer : IVisualizationRenderer
{
    /// <summary>Line colour (default Winamp green).</summary>
    public (byte R, byte G, byte B, byte A) Foreground { get; init; } = (0x00, 0xFF, 0x80, 0xFF);

    /// <summary>Background colour (default black).</summary>
    public (byte R, byte G, byte B, byte A) Background { get; init; } = (0x00, 0x00, 0x00, 0xFF);

    /// <inheritdoc/>
    public string DisplayName => "Oscilloscope";

    /// <inheritdoc/>
    public void Render(in VisualizationInputs inputs, RgbaFrame target)
    {
        ArgumentNullException.ThrowIfNull(target);
        target.Clear(Background.R, Background.G, Background.B, Background.A);

        var samples = inputs.TimeDomainSamples.Span;
        if (samples.Length == 0) return;

        var w = target.Width;
        var h = target.Height;
        var mid = h / 2;

        // Downmix interleaved channels to mono on the fly.
        var channels = Math.Max(1, inputs.Channels);
        var monoLen = samples.Length / channels;
        if (monoLen <= 0) return;

        int prevY = mid;
        for (var x = 0; x < w; x++)
        {
            var srcIdx = (int)((long)x * monoLen / w) * channels;
            if (srcIdx >= samples.Length) break;
            var s = samples[srcIdx];
            if (channels > 1 && srcIdx + 1 < samples.Length) s = (s + samples[srcIdx + 1]) * 0.5f;

            var y = mid - (int)(s * mid);
            if (y < 0) y = 0;
            if (y >= h) y = h - 1;

            DrawVerticalLine(target, x, Math.Min(prevY, y), Math.Max(prevY, y));
            prevY = y;
        }
    }

    private void DrawVerticalLine(RgbaFrame f, int x, int y0, int y1)
    {
        for (var y = y0; y <= y1; y++)
            f.SetPixel(x, y, Foreground.R, Foreground.G, Foreground.B, Foreground.A);
    }
}
