// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using System.Text;

namespace AetherMedia.LocalLibrary.Audio.Visualization.Avs;

/// <summary>
/// Reads classic AVS preset files. Validates the magic header, the
/// "clear every frame" flag, and walks the effect-chain blob list. Effect
/// payloads are kept opaque so we don't have to ship a faithful runtime for
/// every one of AVS's 30+ built-in effects — v1 of the renderer paints an
/// AVS-styled composite that's recognisable without exact preset replay.
/// </summary>
public sealed class AvsPresetParser
{
    private static readonly byte[] Header02 = Encoding.ASCII.GetBytes("Nullsoft AVS Preset 0.2\x1A");

    /// <summary>Parse from a file path.</summary>
    public async Task<AvsPreset> ParseAsync(string filePath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return await ParseAsync(fs, ct).ConfigureAwait(false);
    }

    /// <summary>Parse from a stream.</summary>
    public async Task<AvsPreset> ParseAsync(Stream stream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var buf = new byte[Header02.Length];
        if (await stream.ReadAsync(buf.AsMemory(0, buf.Length), ct).ConfigureAwait(false) != buf.Length)
            throw new FormatException("AVS file is too small to contain a header.");
        if (!buf.AsSpan().SequenceEqual(Header02))
            throw new FormatException("Not an AVS preset (header mismatch).");

        // "Clear every frame" byte.
        var clear = new byte[1];
        if (await stream.ReadAsync(clear.AsMemory(0, 1), ct).ConfigureAwait(false) != 1)
            throw new FormatException("AVS file truncated before clear-flag.");

        var blobs = new List<AvsEffectBlob>();
        var head = new byte[8];
        while (true)
        {
            var read = await stream.ReadAsync(head.AsMemory(0, head.Length), ct).ConfigureAwait(false);
            if (read == 0) break;
            if (read != head.Length) break; // truncated tail — accept what we have

            var typeCode = BinaryPrimitives.ReadInt32LittleEndian(head.AsSpan(0, 4));
            var length   = BinaryPrimitives.ReadInt32LittleEndian(head.AsSpan(4, 4));
            if (length < 0 || length > 16 * 1024 * 1024) break; // sanity

            var payload = new byte[length];
            var got = 0;
            while (got < length)
            {
                var n = await stream.ReadAsync(payload.AsMemory(got, length - got), ct).ConfigureAwait(false);
                if (n == 0) break;
                got += n;
            }
            blobs.Add(new AvsEffectBlob(typeCode, payload));
        }

        return new AvsPreset(
            FormatVersion: "0.2",
            ClearEveryFrame: clear[0] != 0,
            EffectBlobs: blobs);
    }
}
