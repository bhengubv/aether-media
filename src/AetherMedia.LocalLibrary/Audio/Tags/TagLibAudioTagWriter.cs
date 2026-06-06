// SPDX-License-Identifier: MIT

using TagLibFile = TagLib.File;

namespace AetherMedia.LocalLibrary.Audio.Tags;

/// <summary>
/// Universal tag writer — counterpart to <see cref="TagLibAudioTagReader"/>.
/// Updates ID3v1/v2, Vorbis comments, APE, and MP4 atoms via TagLibSharp,
/// preserving every field of the original tag block that is not explicitly
/// overwritten.
/// </summary>
public sealed class TagLibAudioTagWriter : IAudioTagWriter
{
    /// <inheritdoc/>
    public Task WriteAsync(string filePath, AudioTags tags, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        ArgumentNullException.ThrowIfNull(tags);
        if (!System.IO.File.Exists(filePath))
            throw new FileNotFoundException("Audio file not found.", filePath);

        using var file = TagLibFile.Create(filePath);
        Apply(file.Tag, tags);
        file.Save();
        return Task.CompletedTask;
    }

    /// <summary>Apply non-null fields from <paramref name="tags"/> onto <paramref name="tag"/>.</summary>
    internal static void Apply(TagLib.Tag tag, AudioTags tags)
    {
        if (tags.Title    is not null) tag.Title    = tags.Title;
        if (tags.Artist   is not null) tag.Performers = [tags.Artist];
        if (tags.Album    is not null) tag.Album    = tags.Album;
        if (tags.Year     is not null) tag.Year     = (uint)tags.Year.Value;
        if (tags.TrackNumber is not null) tag.Track = (uint)tags.TrackNumber.Value;
        if (tags.Genre    is not null) tag.Genres   = [tags.Genre];
        if (tags.ReplayGainTrackDb is { } gain)
            tag.ReplayGainTrackGain = gain;
        if (tags.ReplayGainTrackPeakDbfs is { } peakDb)
            tag.ReplayGainTrackPeak = Math.Pow(10.0, peakDb / 20.0); // back to linear amplitude
    }
}
