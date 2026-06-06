// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using AetherMedia.LocalLibrary.Audio.Export;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Export;

public class WavExporterTests
{
    [Fact]
    public async Task Exports_ValidRiffWavHeader_AndRoundTripsSamples()
    {
        var tmp = Path.GetTempFileName();
        try
        {
            var samples = new float[] { 0.0f, 0.25f, -0.5f, 1.0f, -1.0f };
            await new WavExporter().ExportAsync(tmp, samples, 48000, channels: 1);

            var bytes = await File.ReadAllBytesAsync(tmp);
            // RIFF header magic
            Assert.Equal((byte)'R', bytes[0]);
            Assert.Equal((byte)'I', bytes[1]);
            Assert.Equal((byte)'F', bytes[2]);
            Assert.Equal((byte)'F', bytes[3]);
            // WAVE
            Assert.Equal((byte)'W', bytes[8]);
            Assert.Equal((byte)'A', bytes[9]);
            // fmt subchunk: format code 3 = IEEE float
            Assert.Equal(3, BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(20, 2)));
            // 32 bits per sample
            Assert.Equal(32, BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(34, 2)));
            // Sample data starts at byte 44
            for (var i = 0; i < samples.Length; i++)
            {
                var val = BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(44 + i * 4, 4));
                Assert.Equal(samples[i], val, 6);
            }
        }
        finally
        {
            File.Delete(tmp);
        }
    }
}
