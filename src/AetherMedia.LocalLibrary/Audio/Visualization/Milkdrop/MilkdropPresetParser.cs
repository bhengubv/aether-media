// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AetherMedia.LocalLibrary.Audio.Visualization.Milkdrop;

/// <summary>
/// Reads a Milkdrop <c>.milk</c> file into a <see cref="MilkdropPreset"/>.
/// The format is INI-ish: a section header (<c>[preset00]</c>), then
/// <c>key=value</c> lines for parameters, plus indexed equation blocks:
/// <list type="bullet">
///   <item><description><c>per_frame_N</c> — per-frame equations.</description></item>
///   <item><description><c>per_pixel_N</c> — per-warp-mesh-vertex equations.</description></item>
///   <item><description><c>shape_K_init_N</c> / <c>shape_K_per_frame_N</c> — custom shape K (1..4).</description></item>
///   <item><description><c>wave_K_init_N</c> / <c>wave_K_per_frame_N</c> / <c>wave_K_per_point_N</c> — custom wave K (1..4).</description></item>
///   <item><description><c>warp_N</c> — HLSL warp-shader body (lines concatenated by index).</description></item>
///   <item><description><c>comp_N</c> — HLSL composite-shader body (lines concatenated by index).</description></item>
/// </list>
/// Comments after <c>//</c> on a line are stripped before parse.
/// </summary>
public sealed class MilkdropPresetParser
{
    // Milkdrop allows both shape_1_init1 (no underscore) and shape_1_init_1 — accept either.
    private static readonly Regex ShapeEqKey =
        new(@"^shape_(\d+)_(init|per_frame)_?(\d+)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex WaveEqKey =
        new(@"^wave_(\d+)_(init|per_frame|per_point)_?(\d+)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ShapeParamKey =
        new(@"^shape_(\d+)_(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex WaveParamKey =
        new(@"^wave_(\d+)_(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
        var shapes = new Dictionary<int, ShapeBuilder>();
        var waves  = new Dictionary<int, WaveBuilder>();
        var warpShader = new SortedDictionary<int, string>();
        var compShader = new SortedDictionary<int, string>();

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        while (await reader.ReadLineAsync(ct).ConfigureAwait(false) is { } raw)
        {
            var line = StripComment(raw).TrimEnd();
            if (line.Length == 0) continue;
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                section = trimmed[1..^1];
                continue;
            }

            var eq = line.IndexOf('=');
            if (eq <= 0) continue;
            var key = line[..eq].Trim();
            var value = line[(eq + 1)..];

            // Top-level equation blocks.
            if (TryIndexedKey(key, "per_frame_", out var pfIdx)) { perFrame[pfIdx] = value.TrimStart(); continue; }
            if (TryIndexedKey(key, "per_pixel_", out var ppIdx)) { perPixel[ppIdx] = value.TrimStart(); continue; }

            // Shader bodies — each warp_N / comp_N line carries one shader fragment.
            if (TryIndexedKey(key, "warp_", out var warpIdx))
            {
                warpShader[warpIdx] = TrimShaderLeadingBacktick(value);
                continue;
            }
            if (TryIndexedKey(key, "comp_", out var compIdx))
            {
                compShader[compIdx] = TrimShaderLeadingBacktick(value);
                continue;
            }

            // Custom shape equations (shape_K_init_N etc).
            var shapeEqMatch = ShapeEqKey.Match(key);
            if (shapeEqMatch.Success)
            {
                var k = int.Parse(shapeEqMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                var kind = shapeEqMatch.Groups[2].Value.ToLowerInvariant();
                var idx = shapeEqMatch.Groups[3].Success
                    ? int.Parse(shapeEqMatch.Groups[3].Value, CultureInfo.InvariantCulture)
                    : 0;
                var sb = shapes.TryGetValue(k, out var existing) ? existing : (shapes[k] = new ShapeBuilder(k));
                (kind == "init" ? sb.InitEqs : sb.PerFrameEqs)[idx] = value.TrimStart();
                continue;
            }

            // Custom wave equations (wave_K_per_point_N etc).
            var waveEqMatch = WaveEqKey.Match(key);
            if (waveEqMatch.Success)
            {
                var k = int.Parse(waveEqMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                var kind = waveEqMatch.Groups[2].Value.ToLowerInvariant();
                var idx = waveEqMatch.Groups[3].Success
                    ? int.Parse(waveEqMatch.Groups[3].Value, CultureInfo.InvariantCulture)
                    : 0;
                var wb = waves.TryGetValue(k, out var existing) ? existing : (waves[k] = new WaveBuilder(k));
                (kind switch
                {
                    "init"        => wb.InitEqs,
                    "per_frame"   => wb.PerFrameEqs,
                    _             => wb.PerPointEqs,
                })[idx] = value.TrimStart();
                continue;
            }

            // Shape / wave numeric parameters (shape_1_enabled, wave_2_r, ...).
            var shapeParamMatch = ShapeParamKey.Match(key);
            if (shapeParamMatch.Success
                && double.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var sv))
            {
                var k = int.Parse(shapeParamMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                var name = shapeParamMatch.Groups[2].Value;
                var sb = shapes.TryGetValue(k, out var existing) ? existing : (shapes[k] = new ShapeBuilder(k));
                sb.Parameters[name] = sv;
                continue;
            }
            var waveParamMatch = WaveParamKey.Match(key);
            if (waveParamMatch.Success
                && double.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var wv))
            {
                var k = int.Parse(waveParamMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                var name = waveParamMatch.Groups[2].Value;
                var wb = waves.TryGetValue(k, out var existing) ? existing : (waves[k] = new WaveBuilder(k));
                wb.Parameters[name] = wv;
                continue;
            }

            // Generic numeric parameter.
            if (double.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                parameters[key] = d;
        }

        return new MilkdropPreset(
            SectionName: section,
            Parameters: parameters,
            PerFrameEquations: perFrame.Values.ToList(),
            PerPixelEquations: perPixel.Values.ToList(),
            Shapes: shapes.Values.OrderBy(s => s.Index).Select(s => s.Build()).ToList(),
            Waves: waves.Values.OrderBy(w => w.Index).Select(w => w.Build()).ToList(),
            WarpShader: warpShader.Count > 0 ? string.Join('\n', warpShader.Values) : null,
            CompositeShader: compShader.Count > 0 ? string.Join('\n', compShader.Values) : null);
    }

    private static string StripComment(string line)
    {
        var idx = line.IndexOf("//", StringComparison.Ordinal);
        return idx < 0 ? line : line[..idx];
    }

    private static string TrimShaderLeadingBacktick(string body)
    {
        // .milk shader lines start with a backtick after =, separating the
        // shader text from the parser's value.
        return body.StartsWith('`') ? body[1..] : body;
    }

    private static bool TryIndexedKey(ReadOnlySpan<char> key, ReadOnlySpan<char> prefix, out int index)
    {
        index = 0;
        if (key.Length <= prefix.Length || !key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;
        return int.TryParse(key[prefix.Length..], NumberStyles.Integer, CultureInfo.InvariantCulture, out index);
    }

    /// <summary>Accumulates one shape definition while parsing.</summary>
    private sealed class ShapeBuilder
    {
        public int Index { get; }
        public Dictionary<string, double> Parameters { get; } = new(StringComparer.OrdinalIgnoreCase);
        public SortedDictionary<int, string> InitEqs { get; } = new();
        public SortedDictionary<int, string> PerFrameEqs { get; } = new();

        public ShapeBuilder(int index) => Index = index;

        public MilkdropCustomShape Build()
        {
            double P(string n, double d = 0) => Parameters.TryGetValue(n, out var v) ? v : d;
            bool B(string n) => Parameters.TryGetValue(n, out var v) && v != 0;

            return new MilkdropCustomShape(
                Index: Index,
                Enabled: B("enabled"),
                Instances: Math.Clamp((int)P("num_inst", 1), 1, 1024),
                Sides: Math.Clamp((int)P("sides", 4), 3, 100),
                Additive: B("additive"),
                ThickOutline: B("thickoutline"),
                X: P("x", 0.5),
                Y: P("y", 0.5),
                Radius: P("rad", 0.1),
                Angle: P("ang"),
                R: P("r"), G: P("g"), B: P("b"), A: P("a", 1.0),
                R2: P("r2"), G2: P("g2"), B2: P("b2"), A2: P("a2", 0.0),
                BorderR: P("border_r"), BorderG: P("border_g"), BorderB: P("border_b"), BorderA: P("border_a", 0),
                InitEquations: InitEqs.Values.ToList(),
                PerFrameEquations: PerFrameEqs.Values.ToList());
        }
    }

    /// <summary>Accumulates one wave definition while parsing.</summary>
    private sealed class WaveBuilder
    {
        public int Index { get; }
        public Dictionary<string, double> Parameters { get; } = new(StringComparer.OrdinalIgnoreCase);
        public SortedDictionary<int, string> InitEqs { get; } = new();
        public SortedDictionary<int, string> PerFrameEqs { get; } = new();
        public SortedDictionary<int, string> PerPointEqs { get; } = new();

        public WaveBuilder(int index) => Index = index;

        public MilkdropCustomWave Build()
        {
            double P(string n, double d = 0) => Parameters.TryGetValue(n, out var v) ? v : d;
            bool B(string n) => Parameters.TryGetValue(n, out var v) && v != 0;

            return new MilkdropCustomWave(
                Index: Index,
                Enabled: B("enabled"),
                Samples: Math.Clamp((int)P("samples", 512), 4, 2048),
                Separation: (int)P("sep"),
                Scaling: P("scaling", 1.0),
                Smoothing: P("smoothing"),
                R: P("r"), G: P("g"), B: P("b"), A: P("a", 1.0),
                Spectrum: B("spectrum"),
                UseDots: B("usedots"),
                ThickOutline: B("thick"),
                Additive: B("additive"),
                InitEquations: InitEqs.Values.ToList(),
                PerFrameEquations: PerFrameEqs.Values.ToList(),
                PerPointEquations: PerPointEqs.Values.ToList());
        }
    }
}
