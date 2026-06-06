// SPDX-License-Identifier: MIT

using TagLibFile = TagLib.File;

namespace AetherMedia.LocalLibrary.Audio.Tags;

/// <summary>
/// Universal tag reader for every audio container Winamp ever opened —
/// ID3v1, ID3v2.3, ID3v2.4 (MP3 / WAV), Vorbis comments (FLAC / OGG),
/// APE tags (Monkey's Audio / WavPack / MPC), and MP4 atoms (M4A / AAC).
/// Implemented as a thin wrapper over TagLibSharp 2.3, which is already a
/// project dependency for video metadata — no second tag library.
///
/// <para>
/// Use <see cref="Id3v2Reader"/> instead when you specifically need a
/// dependency-free ID3v2 reader (e.g. when measuring loudness in a
/// constrained environment); this class is the default everywhere else.
/// </para>
/// </summary>
public sealed class TagLibAudioTagReader : IAudioTagReader
{
    /// <inheritdoc/>
    public Task<AudioTags?> ReadAsync(string filePath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        if (!System.IO.File.Exists(filePath))
            return Task.FromResult<AudioTags?>(null);

        try
        {
            using var file = TagLibFile.Create(filePath);
            return Task.FromResult<AudioTags?>(Project(file));
        }
        catch (TagLib.UnsupportedFormatException)
        {
            return Task.FromResult<AudioTags?>(null);
        }
        catch (TagLib.CorruptFileException)
        {
            return Task.FromResult<AudioTags?>(null);
        }
    }

    /// <inheritdoc/>
    public Task<AudioTags?> ReadAsync(Stream stream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        try
        {
            using var file = TagLibFile.Create(new StreamFileAbstraction(stream));
            return Task.FromResult<AudioTags?>(Project(file));
        }
        catch (TagLib.UnsupportedFormatException)
        {
            return Task.FromResult<AudioTags?>(null);
        }
        catch (TagLib.CorruptFileException)
        {
            return Task.FromResult<AudioTags?>(null);
        }
    }

    /// <summary>
    /// Project a TagLib file's tag block into the player's narrower
    /// <see cref="AudioTags"/> contract.
    /// </summary>
    private static AudioTags Project(TagLibFile file) => ProjectFromTag(file.Tag);

    /// <summary>
    /// Same projection logic against a bare <see cref="TagLib.Tag"/> —
    /// internal so unit tests can exercise it without a backing file.
    /// </summary>
    internal static AudioTags ProjectFromTag(TagLib.Tag tag)
    {
        string? artist = tag.Performers is { Length: > 0 }
            ? tag.Performers.FirstOrDefault(static p => !string.IsNullOrWhiteSpace(p))
            : null;
        string? genre = tag.Genres is { Length: > 0 }
            ? tag.Genres.FirstOrDefault(static g => !string.IsNullOrWhiteSpace(g))
            : null;

        // TagLibSharp stores ReplayGain track gain in dB directly. The peak is
        // a linear amplitude; convert to dBFS to match what LoudnessMeasurement
        // and Id3v2Reader expose.
        double? rgGain = double.IsNaN(tag.ReplayGainTrackGain) ? null : tag.ReplayGainTrackGain;
        double? rgPeak = null;
        if (!double.IsNaN(tag.ReplayGainTrackPeak) && tag.ReplayGainTrackPeak > 0)
            rgPeak = 20.0 * Math.Log10(tag.ReplayGainTrackPeak);

        return new AudioTags(
            Title: string.IsNullOrWhiteSpace(tag.Title) ? null : tag.Title,
            Artist: string.IsNullOrWhiteSpace(artist) ? null : artist,
            Album: string.IsNullOrWhiteSpace(tag.Album) ? null : tag.Album,
            Year: tag.Year == 0 ? null : (int)tag.Year,
            TrackNumber: tag.Track == 0 ? null : (int)tag.Track,
            Genre: string.IsNullOrWhiteSpace(genre) ? null : genre,
            ReplayGainTrackDb: rgGain,
            ReplayGainTrackPeakDbfs: rgPeak);
    }

    /// <summary>
    /// Bridge TagLibSharp's <see cref="TagLib.File.IFileAbstraction"/> to a
    /// caller-owned <see cref="Stream"/>. TagLib reads and writes the same
    /// stream — fine for read-only API surface here.
    /// </summary>
    internal sealed class StreamFileAbstraction : TagLib.File.IFileAbstraction
    {
        public StreamFileAbstraction(Stream stream, string name = "stream")
        {
            ReadStream = stream;
            WriteStream = stream;
            Name = name;
        }

        public string Name { get; }
        public Stream ReadStream { get; }
        public Stream WriteStream { get; }

        public void CloseStream(Stream stream) { /* caller owns the stream */ }
    }
}
