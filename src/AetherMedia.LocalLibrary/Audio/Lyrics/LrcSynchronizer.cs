// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Lyrics;

/// <summary>
/// Maps a playback position to the active lyric line. Holds a binary
/// search index so per-frame lookup stays O(log n) — fine for displaying
/// karaoke lyrics at 60 Hz against a long LRC.
/// </summary>
public sealed class LrcSynchronizer
{
    private readonly LrcFile _file;

    public LrcSynchronizer(LrcFile file)
    {
        _file = file ?? throw new ArgumentNullException(nameof(file));
    }

    /// <summary>
    /// The lyric line that should be visible at <paramref name="position"/>.
    /// Returns null if the position is before the first line.
    /// </summary>
    public LyricLine? GetActiveLine(TimeSpan position)
    {
        if (_file.Lines.Count == 0) return null;
        // Binary search for the largest line whose offset ≤ position.
        var lo = 0;
        var hi = _file.Lines.Count - 1;
        var best = -1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >> 1;
            if (_file.Lines[mid].Offset <= position) { best = mid; lo = mid + 1; }
            else hi = mid - 1;
        }
        return best < 0 ? null : _file.Lines[best];
    }

    /// <summary>The index of the active line, or -1 if before the first line.</summary>
    public int GetActiveIndex(TimeSpan position)
    {
        if (_file.Lines.Count == 0) return -1;
        var lo = 0;
        var hi = _file.Lines.Count - 1;
        var best = -1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >> 1;
            if (_file.Lines[mid].Offset <= position) { best = mid; lo = mid + 1; }
            else hi = mid - 1;
        }
        return best;
    }
}
