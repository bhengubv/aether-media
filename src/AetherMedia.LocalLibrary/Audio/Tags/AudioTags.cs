// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Tags;

/// <summary>
/// Audio tags extracted from a file's metadata block (ID3v2 for MP3/WAV,
/// Vorbis comments for FLAC/OGG, MP4 atoms for AAC). Only the fields that
/// actually matter to the player are surfaced; raw frame access stays inside
/// the reader.
/// </summary>
/// <param name="Title">Track title (TIT2 / TITLE / ©nam).</param>
/// <param name="Artist">Performer (TPE1 / ARTIST / ©ART).</param>
/// <param name="Album">Album name (TALB / ALBUM / ©alb).</param>
/// <param name="Year">Release year, or null.</param>
/// <param name="TrackNumber">Track index within the album, or null.</param>
/// <param name="Genre">Genre string, or null.</param>
/// <param name="ReplayGainTrackDb">
/// Pre-computed track gain in dB (the value of REPLAYGAIN_TRACK_GAIN). Honour
/// this when present rather than re-running the analyser. Null when absent.
/// </param>
/// <param name="ReplayGainTrackPeakDbfs">
/// Pre-computed true peak in dBFS, derived from REPLAYGAIN_TRACK_PEAK
/// (stored as a linear amplitude in the tag). Null when absent.
/// </param>
public sealed record AudioTags(
    string? Title,
    string? Artist,
    string? Album,
    int? Year,
    int? TrackNumber,
    string? Genre,
    double? ReplayGainTrackDb,
    double? ReplayGainTrackPeakDbfs);
