// SPDX-License-Identifier: MIT

using System.Buffers.Binary;

namespace AetherMedia.LocalLibrary.Audio.Export;

/// <summary>
/// Write 32-bit float WAVE (RIFF). No external dependency, no native
/// libraries. Suitable for "save the processed track to disk" features.
/// </summary>
public sealed class WavExporter : IAudioExporter
{
    /// <inheritdoc/>
    public async Task ExportAsync(
        string destinationPath,
        ReadOnlyMemory<float> samples,
        int sampleRateHz,
        int channels,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(destinationPath);
        if (channels < 1) throw new ArgumentOutOfRangeException(nameof(channels));
        if (sampleRateHz <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRateHz));

        var dataBytes = samples.Length * sizeof(float);
        var header = BuildFloatWavHeader(sampleRateHz, channels, dataBytes);

        await using var fs = new FileStream(
            destinationPath, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 64 * 1024, useAsync: true);
        await fs.WriteAsync(header, ct).ConfigureAwait(false);

        // Write samples in little-endian
        var buffer = new byte[64 * 1024];
        var samplesArr = samples.ToArray();
        var offset = 0;
        while (offset < samplesArr.Length)
        {
            var batch = Math.Min(buffer.Length / sizeof(float), samplesArr.Length - offset);
            for (var i = 0; i < batch; i++)
            {
                BinaryPrimitives.WriteSingleLittleEndian(
                    buffer.AsSpan(i * sizeof(float)), samplesArr[offset + i]);
            }
            await fs.WriteAsync(buffer.AsMemory(0, batch * sizeof(float)), ct).ConfigureAwait(false);
            offset += batch;
        }
    }

    private static byte[] BuildFloatWavHeader(int sampleRateHz, int channels, int dataBytes)
    {
        // RIFF + WAVE + fmt (IEEE float, 32-bit) + data
        var header = new byte[44];
        var s = header.AsSpan();

        // RIFF chunk
        "RIFF"u8.CopyTo(s[..4]);
        BinaryPrimitives.WriteInt32LittleEndian(s.Slice(4, 4), 36 + dataBytes);
        "WAVE"u8.CopyTo(s.Slice(8, 4));

        // fmt subchunk
        "fmt "u8.CopyTo(s.Slice(12, 4));
        BinaryPrimitives.WriteInt32LittleEndian(s.Slice(16, 4), 16);       // subchunk size
        BinaryPrimitives.WriteInt16LittleEndian(s.Slice(20, 2), 3);        // IEEE float
        BinaryPrimitives.WriteInt16LittleEndian(s.Slice(22, 2), (short)channels);
        BinaryPrimitives.WriteInt32LittleEndian(s.Slice(24, 4), sampleRateHz);
        BinaryPrimitives.WriteInt32LittleEndian(s.Slice(28, 4), sampleRateHz * channels * 4);
        BinaryPrimitives.WriteInt16LittleEndian(s.Slice(32, 2), (short)(channels * 4));
        BinaryPrimitives.WriteInt16LittleEndian(s.Slice(34, 2), 32);       // bits per sample

        // data subchunk
        "data"u8.CopyTo(s.Slice(36, 4));
        BinaryPrimitives.WriteInt32LittleEndian(s.Slice(40, 4), dataBytes);
        return header;
    }
}
