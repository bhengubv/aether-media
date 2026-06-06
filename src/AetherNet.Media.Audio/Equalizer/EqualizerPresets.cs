// SPDX-License-Identifier: MIT

namespace AetherNet.Media.Audio.Equalizer;

/// <summary>
/// Ten-band graphic-equalizer presets at the ISO 1/3-octave centre frequencies
/// (32, 64, 125, 250, 500, 1k, 2k, 4k, 8k, 16k Hz) — the same band layout
/// Winamp 5, iTunes, and most streaming-app EQs use.
/// </summary>
public static class EqualizerPresets
{
    /// <summary>The 10 ISO 1/3-octave centre frequencies in Hz.</summary>
    public static readonly IReadOnlyList<double> StandardCenterFrequencies =
        [32, 64, 125, 250, 500, 1000, 2000, 4000, 8000, 16000];

    /// <summary>Flat — no boost, no cut.</summary>
    public const string Flat = "Flat";

    /// <summary>Bass-forward curve.</summary>
    public const string BassBoost = "Bass Boost";

    /// <summary>Treble-forward curve.</summary>
    public const string TrebleBoost = "Treble Boost";

    /// <summary>Smile / V-shape — boost low and high, cut mids.</summary>
    public const string Rock = "Rock";

    /// <summary>Lifted highs, slight bass — vocal-focused.</summary>
    public const string Pop = "Pop";

    /// <summary>Warm midrange, gentle highs — jazz / acoustic.</summary>
    public const string Jazz = "Jazz";

    /// <summary>Wide classical curve — gentle low-end, airy highs.</summary>
    public const string Classical = "Classical";

    /// <summary>Vocal clarity — boost 1–4 kHz.</summary>
    public const string Vocal = "Vocal";

    /// <summary>Maps a preset name to its 10-band gain curve (dB per band).</summary>
    public static IReadOnlyList<double> GainsFor(string presetName) => presetName switch
    {
        Flat        => [0, 0, 0, 0, 0, 0, 0, 0, 0, 0],
        BassBoost   => [+6, +5, +4, +2, 0, 0, 0, 0, 0, 0],
        TrebleBoost => [0, 0, 0, 0, 0, 0, +2, +4, +5, +6],
        Rock        => [+5, +4, +2, -1, -3, -2, +1, +4, +5, +5],
        Pop         => [-2, -1, 0, +2, +4, +4, +2, 0, -1, -2],
        Jazz        => [+3, +2, +1, +2, -2, -2, 0, +1, +2, +3],
        Classical   => [+5, +4, +3, +2, -2, -2, 0, +2, +3, +4],
        Vocal       => [-2, -3, -3, -1, +2, +4, +4, +2, 0, -1],
        _           => throw new ArgumentException($"Unknown preset: {presetName}", nameof(presetName)),
    };

    /// <summary>Build the band list from gains (one per <see cref="StandardCenterFrequencies"/> entry).</summary>
    public static IReadOnlyList<EqualizerBand> BandsFor(string presetName)
    {
        var gains = GainsFor(presetName);
        var bands = new EqualizerBand[StandardCenterFrequencies.Count];
        for (var i = 0; i < bands.Length; i++)
            bands[i] = new EqualizerBand(StandardCenterFrequencies[i], gains[i], 1.41);
        return bands;
    }
}
