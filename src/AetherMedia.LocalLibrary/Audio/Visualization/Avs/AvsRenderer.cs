// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization.Avs;

/// <summary>
/// V1 AVS renderer. Reads the parsed <see cref="AvsPreset.ClearEveryFrame"/>
/// flag and the number of effect blobs as a complexity hint, then composes
/// an AVS-styled output: decayed previous frame + additive spectrum bars +
/// oscilloscope line + a colour wash that pulses to the bass band.
///
/// <para>
/// This is "AVS aesthetic" rather than "exact .avs preset replay" — exact
/// replay requires faithful runtimes for AVS's effect catalogue (30+ built-
/// ins plus the user-scriptable "Trans / Super Scope" effects that depend
/// on NS-EEL). The infrastructure is in place; per-effect runtimes are a
/// future v2 wave alongside the matching Milkdrop v2 work.
/// </para>
/// </summary>
public sealed class AvsRenderer : IVisualizationRenderer
{
    private readonly AvsPreset _preset;
    private byte[]? _previous;
    private int _w, _h;
    private double _bassEnergy;

    public AvsRenderer(AvsPreset preset)
    {
        _preset = preset ?? throw new ArgumentNullException(nameof(preset));
    }

    /// <inheritdoc/>
    public string DisplayName => $"AVS preset ({_preset.EffectBlobs.Count} blocks)";

    /// <summary>Decay factor (0..1) applied per frame.</summary>
    public float Decay { get; init; } = 0.92f;

    /// <inheritdoc/>
    public void Render(in VisualizationInputs inputs, RgbaFrame target)
    {
        ArgumentNullException.ThrowIfNull(target);
        EnsureBack(target);

        if (_preset.ClearEveryFrame)
        {
            target.Clear(0, 0, 0, 0xFF);
        }
        else
        {
            // Carry the previous frame forward with decay.
            for (var i = 0; i < _previous!.Length; i += 4)
            {
                target.Pixels[i]     = (byte)(_previous[i]     * Decay);
                target.Pixels[i + 1] = (byte)(_previous[i + 1] * Decay);
                target.Pixels[i + 2] = (byte)(_previous[i + 2] * Decay);
                target.Pixels[i + 3] = 0xFF;
            }
        }

        var bass = ReadBass(inputs.Spectrum);
        // Smoothed bass envelope used for the colour wash.
        _bassEnergy = 0.85 * _bassEnergy + 0.15 * bass;

        AddSpectrumBars(target, inputs.Spectrum);
        AddOscilloscope(target, inputs);
        AddColourWash(target, _bassEnergy);

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

    private static double ReadBass(SpectrumFrame? sp)
    {
        if (sp is not { Magnitudes.Length: > 0 } s) return 0;
        var n = Math.Max(1, s.Magnitudes.Length / 8);
        double sum = 0;
        for (var i = 0; i < n; i++) sum += s.Magnitudes[i];
        return sum / n;
    }

    private void AddSpectrumBars(RgbaFrame target, SpectrumFrame? spectrum)
    {
        if (spectrum is not { Magnitudes.Length: > 0 } sp) return;
        var w = target.Width;
        var h = target.Height;
        var bars = 24;
        var bw = Math.Max(1, w / bars);
        for (var b = 0; b < bars; b++)
        {
            var lo = (int)(sp.Magnitudes.Length * Math.Pow((double)b / bars, 2.0));
            var hi = (int)(sp.Magnitudes.Length * Math.Pow((double)(b + 1) / bars, 2.0));
            if (hi <= lo) hi = lo + 1;
            if (hi > sp.Magnitudes.Length) hi = sp.Magnitudes.Length;

            float peak = 0;
            for (var i = lo; i < hi; i++)
                if (sp.Magnitudes[i] > peak) peak = sp.Magnitudes[i];
            var barH = (int)(Math.Sqrt(peak) * h);
            if (barH <= 0) continue;

            for (var y = h - barH; y < h; y++)
            for (var x = b * bw; x < Math.Min(w, (b + 1) * bw - 1); x++)
            {
                var i = (y * w + x) * 4;
                target.Pixels[i]     = (byte)Math.Min(255, target.Pixels[i] + 32);
                target.Pixels[i + 1] = (byte)Math.Min(255, target.Pixels[i + 1] + 128);
                target.Pixels[i + 2] = (byte)Math.Min(255, target.Pixels[i + 2] + 200);
            }
        }
    }

    private static void AddOscilloscope(RgbaFrame target, in VisualizationInputs inputs)
    {
        var samples = inputs.TimeDomainSamples.Span;
        if (samples.Length == 0) return;
        var w = target.Width;
        var h = target.Height;
        var mid = h / 2;
        var channels = Math.Max(1, inputs.Channels);
        var mono = samples.Length / channels;
        if (mono <= 0) return;

        int prevY = mid;
        for (var x = 0; x < w; x++)
        {
            var idx = (int)((long)x * mono / w) * channels;
            if (idx >= samples.Length) break;
            var s = samples[idx];
            if (channels > 1 && idx + 1 < samples.Length) s = (s + samples[idx + 1]) * 0.5f;
            var y = mid - (int)(s * (mid - 1));
            if (y < 0) y = 0;
            if (y >= h) y = h - 1;
            var lo = Math.Min(prevY, y);
            var hi = Math.Max(prevY, y);
            for (var py = lo; py <= hi; py++)
            {
                var i = (py * w + x) * 4;
                target.Pixels[i]     = (byte)Math.Min(255, target.Pixels[i] + 0xC0);
                target.Pixels[i + 1] = (byte)Math.Min(255, target.Pixels[i + 1] + 0x80);
                target.Pixels[i + 2] = (byte)Math.Min(255, target.Pixels[i + 2] + 0x20);
            }
            prevY = y;
        }
    }

    private static void AddColourWash(RgbaFrame target, double bass)
    {
        var amount = Math.Clamp(bass * 3.0, 0.0, 1.0);
        if (amount < 0.05) return;
        var r = (byte)(60 * amount);
        var g = (byte)(0 * amount);
        var b = (byte)(40 * amount);
        for (var i = 0; i < target.Pixels.Length; i += 4)
        {
            target.Pixels[i]     = (byte)Math.Min(255, target.Pixels[i]     + r);
            target.Pixels[i + 1] = (byte)Math.Min(255, target.Pixels[i + 1] + g);
            target.Pixels[i + 2] = (byte)Math.Min(255, target.Pixels[i + 2] + b);
        }
    }
}
