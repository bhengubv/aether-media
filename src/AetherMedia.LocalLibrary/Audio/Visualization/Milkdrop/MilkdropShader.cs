// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text.RegularExpressions;

namespace AetherMedia.LocalLibrary.Audio.Visualization.Milkdrop;

/// <summary>
/// CPU interpreter for the documented subset of HLSL Milkdrop 2 preset
/// shader bodies. The Milkdrop shader vocabulary in practice is small and
/// stereotyped — almost every <c>warp_*</c> / <c>comp_*</c> body falls into
/// one of these forms:
///
/// <list type="bullet">
///   <item><description><c>ret = tex2D(sampler_main, uv);</c> — sample the previous frame at this pixel.</description></item>
///   <item><description><c>ret.rgb *= 0.95;</c> / <c>ret.rgb = ret.rgb * 0.95;</c> — scalar multiply.</description></item>
///   <item><description><c>ret.rgb += float3(0.1, 0.05, 0.2);</c> — additive colour offset.</description></item>
///   <item><description><c>ret = lerp(ret, float4(R,G,B,A), t);</c> — colour blend.</description></item>
///   <item><description><c>ret.a = 1;</c> — explicit alpha set.</description></item>
/// </list>
///
/// <para>
/// Statements that don't match a known pattern are skipped (logged via
/// <see cref="UnrecognisedStatementCount"/>). Anything more complex than
/// the catalogue above — branching, custom samplers, real float4 maths —
/// is the territory of a proper GPU pipeline; this CPU interpreter
/// targets the 80% case that runs on the warp/composite layer of stock
/// Milkdrop 2 presets.
/// </para>
/// </summary>
public sealed class MilkdropShader
{
    private static readonly Regex BodyRegex =
        new(@"shader_body\s*\{([\s\S]*?)\}", RegexOptions.Compiled);

    private static readonly Regex AssignTex2D =
        new(@"^(?<lhs>ret(?:\.(?:r|g|b|a|x|y|z|w|rgb|rgba|xyz|xyzw))?)\s*=\s*tex2D\s*\(\s*\w+\s*,\s*uv\s*\)$",
            RegexOptions.Compiled);

    private static readonly Regex CompoundOp =
        new(@"^(?<lhs>ret(?:\.(?:r|g|b|a|x|y|z|w|rgb|rgba|xyz|xyzw))?)\s*(?<op>\*=|\+=|-=|/=)\s*(?<rhs>.+)$",
            RegexOptions.Compiled);

    private static readonly Regex BinaryAssign =
        new(@"^(?<lhs>ret(?:\.(?:r|g|b|a|x|y|z|w|rgb|rgba|xyz|xyzw))?)\s*=\s*(?<a>ret(?:\.(?:r|g|b|a|x|y|z|w|rgb|rgba|xyz|xyzw))?)\s*(?<op>\*|\+|-|/)\s*(?<b>.+)$",
            RegexOptions.Compiled);

    private static readonly Regex DirectAssign =
        new(@"^(?<lhs>ret(?:\.(?:r|g|b|a|x|y|z|w|rgb|rgba|xyz|xyzw))?)\s*=\s*(?<rhs>.+)$",
            RegexOptions.Compiled);

    private static readonly Regex LerpAssign =
        new(@"^(?<lhs>ret(?:\.(?:r|g|b|a|x|y|z|w|rgb|rgba|xyz|xyzw))?)\s*=\s*lerp\s*\(\s*(?<a>.+?)\s*,\s*(?<b>.+?)\s*,\s*(?<t>.+?)\s*\)$",
            RegexOptions.Compiled);

    private readonly List<Statement> _statements;

    private MilkdropShader(List<Statement> statements, int unrecognised)
    {
        _statements = statements;
        UnrecognisedStatementCount = unrecognised;
    }

    /// <summary>Number of statements parsed but not recognised (skipped at render time).</summary>
    public int UnrecognisedStatementCount { get; }

    /// <summary>Number of statements the interpreter understands and will execute.</summary>
    public int RecognisedStatementCount => _statements.Count;

    /// <summary>
    /// Compile a shader source body. Returns null when no statements were
    /// recognised — the renderer treats null as "no shader" and skips the
    /// pass.
    /// </summary>
    public static MilkdropShader? TryCompile(string? source)
    {
        if (string.IsNullOrWhiteSpace(source)) return null;

        string body = source;
        var match = BodyRegex.Match(source);
        if (match.Success) body = match.Groups[1].Value;

        var statements = new List<Statement>();
        var unrecognised = 0;
        foreach (var raw in body.Split(';'))
        {
            var s = StripLine(raw);
            if (string.IsNullOrEmpty(s)) continue;
            if (TryParseStatement(s, out var stmt))
                statements.Add(stmt!);
            else
                unrecognised++;
        }

        return statements.Count == 0 ? null : new MilkdropShader(statements, unrecognised);
    }

    /// <summary>Run the shader against every pixel of <paramref name="target"/>.</summary>
    public void RenderPerPixel(RgbaFrame target, byte[] previousFrame, MilkdropEvaluator evaluator)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(previousFrame);
        ArgumentNullException.ThrowIfNull(evaluator);

        var w = target.Width;
        var h = target.Height;
        if (previousFrame.Length < w * h * 4) return;

        Span<double> ret = stackalloc double[4];

        for (var y = 0; y < h; y++)
        {
            var uvY = (double)y / Math.Max(1, h - 1);
            for (var x = 0; x < w; x++)
            {
                var uvX = (double)x / Math.Max(1, w - 1);
                var ti = (y * w + x) * 4;
                ret[0] = target.Pixels[ti]     / 255.0;
                ret[1] = target.Pixels[ti + 1] / 255.0;
                ret[2] = target.Pixels[ti + 2] / 255.0;
                ret[3] = target.Pixels[ti + 3] / 255.0;

                foreach (var stmt in _statements)
                    stmt.Apply(ret, uvX, uvY, previousFrame, w, h);

                target.Pixels[ti]     = (byte)Math.Clamp(ret[0] * 255, 0, 255);
                target.Pixels[ti + 1] = (byte)Math.Clamp(ret[1] * 255, 0, 255);
                target.Pixels[ti + 2] = (byte)Math.Clamp(ret[2] * 255, 0, 255);
                target.Pixels[ti + 3] = (byte)Math.Clamp(ret[3] * 255, 0, 255);
            }
        }
    }

    private static string StripLine(string raw)
    {
        var s = raw.Trim();
        var slash = s.IndexOf("//", StringComparison.Ordinal);
        if (slash >= 0) s = s[..slash];
        // Drop type prefixes: `float4 ret = ...` → `ret = ...`.
        s = Regex.Replace(s, @"^\s*(float\d?|half\d?)\s+", "");
        return s.Trim();
    }

    private static bool TryParseStatement(string text, out Statement? statement)
    {
        statement = null;

        var lerp = LerpAssign.Match(text);
        if (lerp.Success)
        {
            var maskA = MaskFor(lerp.Groups["lhs"].Value);
            var aColour = TryParseColour(lerp.Groups["a"].Value);
            var bColour = TryParseColour(lerp.Groups["b"].Value);
            if (!TryParseScalar(lerp.Groups["t"].Value, out var t) || aColour is null || bColour is null) return false;
            statement = new LerpStatement(maskA, aColour.Value, bColour.Value, t);
            return true;
        }

        var tex = AssignTex2D.Match(text);
        if (tex.Success)
        {
            statement = new Tex2DStatement(MaskFor(tex.Groups["lhs"].Value));
            return true;
        }

        var compound = CompoundOp.Match(text);
        if (compound.Success)
        {
            var mask = MaskFor(compound.Groups["lhs"].Value);
            var op = compound.Groups["op"].Value[..1];
            var rhsColour = TryParseColour(compound.Groups["rhs"].Value);
            if (rhsColour is null) return false;
            statement = new CompoundStatement(mask, op[0], rhsColour.Value);
            return true;
        }

        var bin = BinaryAssign.Match(text);
        if (bin.Success)
        {
            var mask = MaskFor(bin.Groups["lhs"].Value);
            var op = bin.Groups["op"].Value[0];
            var rhsColour = TryParseColour(bin.Groups["b"].Value);
            if (rhsColour is null) return false;
            statement = new CompoundStatement(mask, op, rhsColour.Value);
            return true;
        }

        var direct = DirectAssign.Match(text);
        if (direct.Success)
        {
            var mask = MaskFor(direct.Groups["lhs"].Value);
            var rhsColour = TryParseColour(direct.Groups["rhs"].Value);
            if (rhsColour is null) return false;
            statement = new ConstStatement(mask, rhsColour.Value);
            return true;
        }

        return false;
    }

    private static int[] MaskFor(string lhs)
    {
        var dot = lhs.IndexOf('.');
        if (dot < 0) return new[] { 0, 1, 2, 3 };
        var sub = lhs[(dot + 1)..];
        var result = new List<int>(sub.Length);
        foreach (var c in sub)
        {
            result.Add(char.ToLowerInvariant(c) switch
            {
                'r' or 'x' => 0,
                'g' or 'y' => 1,
                'b' or 'z' => 2,
                'a' or 'w' => 3,
                _ => 0,
            });
        }
        return result.ToArray();
    }

    private static (double R, double G, double B, double A)? TryParseColour(string text)
    {
        var t = text.Trim();
        // float3(a,b,c) / float4(a,b,c,d)
        var m = Regex.Match(t, @"^float([234])\s*\(\s*([^)]+)\s*\)$");
        if (m.Success)
        {
            var args = m.Groups[2].Value.Split(',', StringSplitOptions.TrimEntries);
            if (args.Length < 3) return null;
            if (!TryParseScalar(args[0], out var r)) return null;
            if (!TryParseScalar(args[1], out var g)) return null;
            if (!TryParseScalar(args[2], out var b)) return null;
            var a = args.Length >= 4 && TryParseScalar(args[3], out var aa) ? aa : 1.0;
            return (r, g, b, a);
        }
        if (TryParseScalar(t, out var scalar)) return (scalar, scalar, scalar, scalar);
        return null;
    }

    private static bool TryParseScalar(string text, out double value) =>
        double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    private abstract class Statement
    {
        public abstract void Apply(Span<double> ret, double uvX, double uvY, byte[] prev, int w, int h);
    }

    private sealed class Tex2DStatement : Statement
    {
        private readonly int[] _mask;

        public Tex2DStatement(int[] mask) => _mask = mask;

        public override void Apply(Span<double> ret, double uvX, double uvY, byte[] prev, int w, int h)
        {
            // Bilinear sample.
            var sx = uvX * (w - 1);
            var sy = uvY * (h - 1);
            var x0 = (int)sx; var y0 = (int)sy;
            var fx = sx - x0; var fy = sy - y0;
            var x1 = Math.Min(w - 1, x0 + 1);
            var y1 = Math.Min(h - 1, y0 + 1);
            for (var c = 0; c < 4; c++)
            {
                double i00 = prev[(y0 * w + x0) * 4 + c];
                double i10 = prev[(y0 * w + x1) * 4 + c];
                double i01 = prev[(y1 * w + x0) * 4 + c];
                double i11 = prev[(y1 * w + x1) * 4 + c];
                var top = i00 * (1 - fx) + i10 * fx;
                var bot = i01 * (1 - fx) + i11 * fx;
                var sampled = (top * (1 - fy) + bot * fy) / 255.0;
                if (Contains(_mask, c)) ret[c] = sampled;
            }
        }
    }

    private sealed class CompoundStatement : Statement
    {
        private readonly int[] _mask;
        private readonly char _op;
        private readonly (double R, double G, double B, double A) _rhs;

        public CompoundStatement(int[] mask, char op, (double R, double G, double B, double A) rhs)
        {
            _mask = mask;
            _op = op;
            _rhs = rhs;
        }

        public override void Apply(Span<double> ret, double uvX, double uvY, byte[] prev, int w, int h)
        {
            for (var i = 0; i < _mask.Length; i++)
            {
                var c = _mask[i];
                var rhsValue = c switch { 0 => _rhs.R, 1 => _rhs.G, 2 => _rhs.B, _ => _rhs.A };
                ret[c] = _op switch
                {
                    '*' => ret[c] * rhsValue,
                    '+' => ret[c] + rhsValue,
                    '-' => ret[c] - rhsValue,
                    '/' => rhsValue == 0 ? ret[c] : ret[c] / rhsValue,
                    _   => ret[c],
                };
            }
        }
    }

    private sealed class ConstStatement : Statement
    {
        private readonly int[] _mask;
        private readonly (double R, double G, double B, double A) _value;

        public ConstStatement(int[] mask, (double R, double G, double B, double A) value)
        {
            _mask = mask;
            _value = value;
        }

        public override void Apply(Span<double> ret, double uvX, double uvY, byte[] prev, int w, int h)
        {
            for (var i = 0; i < _mask.Length; i++)
            {
                var c = _mask[i];
                ret[c] = c switch { 0 => _value.R, 1 => _value.G, 2 => _value.B, _ => _value.A };
            }
        }
    }

    private sealed class LerpStatement : Statement
    {
        private readonly int[] _mask;
        private readonly (double R, double G, double B, double A) _a;
        private readonly (double R, double G, double B, double A) _b;
        private readonly double _t;

        public LerpStatement(int[] mask, (double R, double G, double B, double A) a, (double R, double G, double B, double A) b, double t)
        {
            _mask = mask;
            _a = a;
            _b = b;
            _t = Math.Clamp(t, 0.0, 1.0);
        }

        public override void Apply(Span<double> ret, double uvX, double uvY, byte[] prev, int w, int h)
        {
            for (var i = 0; i < _mask.Length; i++)
            {
                var c = _mask[i];
                var av = c switch { 0 => _a.R, 1 => _a.G, 2 => _a.B, _ => _a.A };
                var bv = c switch { 0 => _b.R, 1 => _b.G, 2 => _b.B, _ => _b.A };
                ret[c] = av * (1 - _t) + bv * _t;
            }
        }
    }

    private static bool Contains(int[] arr, int value)
    {
        for (var i = 0; i < arr.Length; i++) if (arr[i] == value) return true;
        return false;
    }
}
