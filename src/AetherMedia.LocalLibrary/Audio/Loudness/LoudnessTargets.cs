// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Loudness;

/// <summary>
/// Reference loudness targets used by the major streaming and broadcast
/// platforms. All values are in LUFS (Loudness Units relative to Full Scale)
/// per ITU-R BS.1770-4 / EBU R128.
/// </summary>
public static class LoudnessTargets
{
    /// <summary>Spotify Normal preset: −14 LUFS.</summary>
    public const double Spotify = -14.0;

    /// <summary>Spotify Quiet preset: −19 LUFS.</summary>
    public const double SpotifyQuiet = -19.0;

    /// <summary>Spotify Loud preset: −11 LUFS.</summary>
    public const double SpotifyLoud = -11.0;

    /// <summary>YouTube auto-normalisation target: −14 LUFS.</summary>
    public const double YouTube = -14.0;

    /// <summary>Apple Music Sound Check target: −16 LUFS.</summary>
    public const double AppleMusic = -16.0;

    /// <summary>Tidal default Normalise target: −14 LUFS.</summary>
    public const double Tidal = -14.0;

    /// <summary>Amazon Music HD / Ultra HD target: −14 LUFS.</summary>
    public const double AmazonMusic = -14.0;

    /// <summary>EBU R128 broadcast specification (Europe): −23 LUFS.</summary>
    public const double EbuR128Broadcast = -23.0;

    /// <summary>ATSC A/85 broadcast specification (US): −24 LUFS.</summary>
    public const double AtscA85Broadcast = -24.0;

    /// <summary>Default target when no platform is specified: −14 LUFS.</summary>
    public const double Default = Spotify;
}
