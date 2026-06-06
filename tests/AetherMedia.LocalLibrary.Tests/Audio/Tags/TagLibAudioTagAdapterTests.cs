// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Tags;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Tags;

/// <summary>
/// Tests for the <see cref="TagLibAudioTagReader"/> / <see cref="TagLibAudioTagWriter"/>
/// projection logic. Exercising TagLibSharp itself is the upstream project's
/// job — here we only confirm our mapping between <see cref="TagLib.Tag"/>
/// and <see cref="AudioTags"/> is correct.
/// </summary>
public class TagLibAudioTagAdapterTests
{
    [Fact]
    public void Reader_Project_FillsAllFields_FromTag()
    {
        var src = new FakeTag
        {
            Title = "Sample",
            Performers = ["Artist"],
            Album = "Album",
            Year = 2023,
            Track = 4,
            Genres = ["Rock"],
            ReplayGainTrackGain = -6.5,
            ReplayGainTrackPeak = 0.8,
        };

        var tags = TagLibAudioTagReader.ProjectFromTag(src);

        Assert.Equal("Sample", tags.Title);
        Assert.Equal("Artist", tags.Artist);
        Assert.Equal("Album",  tags.Album);
        Assert.Equal(2023,     tags.Year);
        Assert.Equal(4,        tags.TrackNumber);
        Assert.Equal("Rock",   tags.Genre);
        Assert.Equal(-6.5,     tags.ReplayGainTrackDb);
        Assert.NotNull(tags.ReplayGainTrackPeakDbfs);
        Assert.Equal(20.0 * Math.Log10(0.8), tags.ReplayGainTrackPeakDbfs!.Value, 3);
    }

    [Fact]
    public void Reader_ProjectsEmptyStringsAsNull()
    {
        var src = new FakeTag
        {
            Title = "",
            Performers = [""],
            Album = "   ",
            Genres = [],
            ReplayGainTrackGain = double.NaN,
            ReplayGainTrackPeak = double.NaN,
        };

        var tags = TagLibAudioTagReader.ProjectFromTag(src);

        Assert.Null(tags.Title);
        Assert.Null(tags.Artist);
        Assert.Null(tags.Album);
        Assert.Null(tags.Genre);
        Assert.Null(tags.ReplayGainTrackDb);
        Assert.Null(tags.ReplayGainTrackPeakDbfs);
    }

    [Fact]
    public void Writer_Apply_SetsAllFields()
    {
        var dest = new FakeTag();
        var tags = new AudioTags(
            Title: "Sample",
            Artist: "Artist",
            Album: "Album",
            Year: 1999,
            TrackNumber: 7,
            Genre: "Pop",
            ReplayGainTrackDb: -2.0,
            ReplayGainTrackPeakDbfs: 20.0 * Math.Log10(0.9));

        TagLibAudioTagWriter.Apply(dest, tags);

        Assert.Equal("Sample", dest.Title);
        Assert.Equal(new[] { "Artist" }, dest.Performers);
        Assert.Equal("Album", dest.Album);
        Assert.Equal((uint)1999, dest.Year);
        Assert.Equal((uint)7, dest.Track);
        Assert.Equal(new[] { "Pop" }, dest.Genres);
        Assert.Equal(-2.0, dest.ReplayGainTrackGain, 6);
        Assert.Equal(0.9,  dest.ReplayGainTrackPeak, 4);
    }

    /// <summary>
    /// In-memory <see cref="TagLib.Tag"/> implementation. TagLibSharp's base
    /// class is abstract; only Tag-level fields matter to our projection
    /// logic, so we override exactly those.
    /// </summary>
    private sealed class FakeTag : TagLib.Tag
    {
        private string? _title;
        private string[] _performers = [];
        private string? _album;
        private uint _year;
        private uint _track;
        private string[] _genres = [];
        private double _replayGainTrackGain = double.NaN;
        private double _replayGainTrackPeak = double.NaN;

        public override TagLib.TagTypes TagTypes => TagLib.TagTypes.Id3v2;
        public override string? Title { get => _title; set => _title = value; }
        public override string[] Performers { get => _performers; set => _performers = value ?? []; }
        public override string? Album { get => _album; set => _album = value; }
        public override uint Year { get => _year; set => _year = value; }
        public override uint Track { get => _track; set => _track = value; }
        public override string[] Genres { get => _genres; set => _genres = value ?? []; }
        public override double ReplayGainTrackGain { get => _replayGainTrackGain; set => _replayGainTrackGain = value; }
        public override double ReplayGainTrackPeak { get => _replayGainTrackPeak; set => _replayGainTrackPeak = value; }
        public override void Clear()
        {
            _title = null; _performers = []; _album = null; _year = 0; _track = 0;
            _genres = []; _replayGainTrackGain = double.NaN; _replayGainTrackPeak = double.NaN;
        }
    }
}
