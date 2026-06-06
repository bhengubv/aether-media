// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Effects;

namespace AetherMedia.LocalLibrary.Audio.Equalizer;

/// <summary>
/// N-band parametric equalizer. Implementations apply biquad peaking filters
/// using the RBJ Audio EQ Cookbook formulas, processing PCM in place.
/// </summary>
public interface IEqualizer : IDspEffect
{
    /// <summary>The bands currently configured on the equalizer.</summary>
    IReadOnlyList<EqualizerBand> Bands { get; }

    /// <summary>Replace all bands.</summary>
    void SetBands(IEnumerable<EqualizerBand> bands);

    /// <summary>Apply a named preset (e.g. "Bass Boost", "Flat", "Rock").</summary>
    void ApplyPreset(string presetName);

    /// <summary>All preset names this equalizer ships with.</summary>
    IReadOnlyList<string> AvailablePresets { get; }
}
