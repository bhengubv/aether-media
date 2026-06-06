// SPDX-License-Identifier: MIT

using System.Text;
using AetherMedia.LocalLibrary.Audio.Tags;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Tags;

public class Id3v2ReaderTests
{
    [Fact]
    public async Task ReadsTitle_ArtistFromIso8859_1Frames()
    {
        var bytes = new Id3Builder(version: 4)
            .AddTextFrame("TIT2", encoding: 0, "Hello World")
            .AddTextFrame("TPE1", encoding: 0, "An Artist")
            .Build();
        using var ms = new MemoryStream(bytes);

        var tags = await new Id3v2Reader().ReadAsync(ms);
        Assert.NotNull(tags);
        Assert.Equal("Hello World", tags!.Title);
        Assert.Equal("An Artist", tags.Artist);
    }

    [Fact]
    public async Task ReadsReplayGainFromTxxxFrame()
    {
        // ID3v2 TXXX:  encoding | description \0 | value \0
        var desc  = "REPLAYGAIN_TRACK_GAIN";
        var value = "-6.50 dB";
        var payload = new List<byte> { 0 };
        payload.AddRange(Encoding.GetEncoding("ISO-8859-1").GetBytes(desc));
        payload.Add(0);
        payload.AddRange(Encoding.GetEncoding("ISO-8859-1").GetBytes(value));

        var bytes = new Id3Builder(version: 4)
            .AddRawFrame("TXXX", payload.ToArray())
            .Build();
        using var ms = new MemoryStream(bytes);

        var tags = await new Id3v2Reader().ReadAsync(ms);
        Assert.NotNull(tags);
        Assert.Equal(-6.5, tags!.ReplayGainTrackDb!.Value, 3);
    }

    [Fact]
    public async Task ReadsReplayGainPeakAsDbfs()
    {
        var desc  = "REPLAYGAIN_TRACK_PEAK";
        var value = "0.8"; // linear amplitude ≈ -1.94 dBFS
        var payload = new List<byte> { 0 };
        payload.AddRange(Encoding.GetEncoding("ISO-8859-1").GetBytes(desc));
        payload.Add(0);
        payload.AddRange(Encoding.GetEncoding("ISO-8859-1").GetBytes(value));

        var bytes = new Id3Builder(version: 4)
            .AddRawFrame("TXXX", payload.ToArray())
            .Build();
        using var ms = new MemoryStream(bytes);

        var tags = await new Id3v2Reader().ReadAsync(ms);
        Assert.NotNull(tags);
        var expectedDbfs = 20.0 * Math.Log10(0.8);
        Assert.Equal(expectedDbfs, tags!.ReplayGainTrackPeakDbfs!.Value, 3);
    }

    [Fact]
    public async Task ReturnsNullWhenNoId3Header()
    {
        using var ms = new MemoryStream(new byte[] { 0xFF, 0xFB, 0x00, 0x00 }); // MP3 sync, no ID3
        Assert.Null(await new Id3v2Reader().ReadAsync(ms));
    }

    /// <summary>
    /// Minimal ID3v2 (v3 or v4) builder for tests — emits the header + a stream
    /// of frames. Synchsafe size for v4 tag size; standard 32-bit size for v3
    /// frame headers.
    /// </summary>
    private sealed class Id3Builder
    {
        private readonly byte _version;
        private readonly List<byte> _frames = [];

        public Id3Builder(int version) { _version = (byte)version; }

        public Id3Builder AddTextFrame(string id, byte encoding, string text)
        {
            var bytes = encoding == 0
                ? Encoding.GetEncoding("ISO-8859-1").GetBytes(text)
                : Encoding.UTF8.GetBytes(text);
            var payload = new byte[1 + bytes.Length];
            payload[0] = encoding;
            Buffer.BlockCopy(bytes, 0, payload, 1, bytes.Length);
            return AddRawFrame(id, payload);
        }

        public Id3Builder AddRawFrame(string id, byte[] payload)
        {
            if (id.Length != 4) throw new ArgumentException("Frame id must be 4 chars.");
            _frames.AddRange(Encoding.ASCII.GetBytes(id));
            var size = payload.Length;
            if (_version == 4)
            {
                _frames.Add((byte)((size >> 21) & 0x7F));
                _frames.Add((byte)((size >> 14) & 0x7F));
                _frames.Add((byte)((size >> 7)  & 0x7F));
                _frames.Add((byte)(size & 0x7F));
            }
            else
            {
                _frames.Add((byte)((size >> 24) & 0xFF));
                _frames.Add((byte)((size >> 16) & 0xFF));
                _frames.Add((byte)((size >> 8)  & 0xFF));
                _frames.Add((byte)(size & 0xFF));
            }
            _frames.Add(0); _frames.Add(0); // flags
            _frames.AddRange(payload);
            return this;
        }

        public byte[] Build()
        {
            var tagSize = _frames.Count;
            var header = new byte[]
            {
                (byte)'I', (byte)'D', (byte)'3', _version, 0, 0,
                (byte)((tagSize >> 21) & 0x7F),
                (byte)((tagSize >> 14) & 0x7F),
                (byte)((tagSize >> 7)  & 0x7F),
                (byte)(tagSize & 0x7F),
            };
            var combined = new byte[header.Length + _frames.Count];
            Buffer.BlockCopy(header, 0, combined, 0, header.Length);
            _frames.CopyTo(combined, header.Length);
            return combined;
        }
    }
}
