// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization.Milkdrop;

/// <summary>
/// Typed view over the loose <c>name → double</c> bag inside a
/// <see cref="MilkdropPreset"/>. Names mirror Milkdrop's documented preset
/// variables exactly — case-insensitive lookup so handwritten preset files
/// with inconsistent casing still resolve.
/// </summary>
public sealed class MilkdropParameters
{
    private readonly Dictionary<string, double> _bag;

    public MilkdropParameters(IReadOnlyDictionary<string, double> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _bag = new Dictionary<string, double>(source, StringComparer.OrdinalIgnoreCase);
    }

    public double Get(string name, double fallback = 0.0) =>
        _bag.TryGetValue(name, out var v) ? v : fallback;

    public bool GetBool(string name, bool fallback = false) =>
        _bag.TryGetValue(name, out var v) ? v != 0.0 : fallback;

    public int GetInt(string name, int fallback = 0) =>
        _bag.TryGetValue(name, out var v) ? (int)v : fallback;

    // ── Frame-level dynamics (preset start values; equations mutate copies) ──
    public double Zoom   => Get("zoom",  1.0);
    public double Rot    => Get("rot",   0.0);
    public double Cx     => Get("cx",    0.5);
    public double Cy     => Get("cy",    0.5);
    public double Dx     => Get("dx",    0.0);
    public double Dy     => Get("dy",    0.0);
    public double Sx     => Get("sx",    1.0);
    public double Sy     => Get("sy",    1.0);
    public double Warp   => Get("warp",  0.0);
    public double Decay  => Get("fDecay", 0.96);

    public double WaveAlpha => Get("fWaveAlpha", 0.8);
    public double WaveScale => Get("fWaveScale", 2.33);
    public double WaveR     => Get("wave_r", 0.65);
    public double WaveG     => Get("wave_g", 0.65);
    public double WaveB     => Get("wave_b", 0.65);
    public double WaveX     => Get("wave_x", 0.5);
    public double WaveY     => Get("wave_y", 0.5);

    public int    WaveMode  => GetInt("nWaveMode", 0);
    public bool   AdditiveWaves         => GetBool("bAdditiveWaves");
    public bool   MaximizeWaveColor     => GetBool("bMaximizeWaveColor", true);
    public bool   ModWaveAlphaByVolume  => GetBool("bModWaveAlphaByVolume");
    public bool   DarkenCenter          => GetBool("bDarkenCenter");
    public bool   Brighten              => GetBool("bBrighten");
    public bool   Darken                => GetBool("bDarken");
}
