// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text;

namespace AetherMedia.LocalLibrary.Audio.Visualization.Milkdrop;

/// <summary>
/// Reads a Milkdrop <c>.milk</c> file into a <see cref="MilkdropPreset"/>.
/// The format is INI-ish: a section header (<c>[preset00]</c>), then
/// <c>key=value</c> lines for parameters, plus <c>per_frame_N=equation</c>
/// and <c>per_pixel_N=equation</c> lines.
///
/// <para>
/// Warp / composite HLSL bodies (<c>warp_1=`...</c>) are recognised but
/// stored opaque — the v1 CPU renderer doesn't run them. Comments after
/// <c>//</c> on a line are stripped before parse.
/// </para>
/// </summary>
public sealed class MilkdropPresetParser
{
    /// <summary>Parse from a file path.</summary>
    public async Task<MilkdropPreset> ParseAsync(string filePath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return await ParseAsync(fs, ct).ConfigureAwait(false);
    }

    /// <summary>Parse from a stream (UTF-8 / ASCII text).</summary>
    public async Task<MilkdropPreset> ParseAsync(Stream stream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var section = "preset00";
        var parameters = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var perFrame = new SortedDictionary<int, string>();
        var perPixel = new SortedDictionary<int, string>();

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        while (await reader.ReadLineAsync(ct).ConfigureAwait(false) is { } raw)
        {
            var line = StripComment(raw).Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                section = line[1..^1];
                continue;
            }

            var eq = line.IndexOf('=');
            if (eq <= 0) continue;
            var key = line[..eq].Trim();
            var value = line[(eq + 1)..].TrimStart();

            // Equation lines.
            if (TryIndexedKey(key, "per_frame_", out var pfIdx)) { perFrame[pfIdx] = value; continue; }
            if (TryIndexedKey(key, "per_pixel_", out var ppIdx)) { perPixel[ppIdx] = value; continue; }

            // Shader bodies — keep as parameters for now, opaque.
            if (key.StartsWith("warp_", StringComparison.OrdinalIgnoreCase) ||
                key.StartsWith("comp_", StringComparison.OrdinalIgnoreCase))
                continue;

            // Numeric parameter.
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                parameters[key] = d;
        }

        return new MilkdropPreset(
            SectionName: section,
            Parameters: parameters,
            PerFrameEquations: perFrame.Values.ToList(),
            PerPixelEquations: perPixel.Values.ToList());
    }

    private static string StripComment(string line)
    {
        var idx = line.IndexOf("//", StringComparison.Ordinal);
        return idx < 0 ? line : line[..idx];
    }

    private static bool TryIndexedKey(ReadOnlySpan<char> key, ReadOnlySpan<char> prefix, out int index)
    {
        index = 0;
        if (key.Length <= prefix.Length || !key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;
        return int.TryParse(key[prefix.Length..], NumberStyles.Integer, CultureInfo.InvariantCulture, out index);
    }
}
