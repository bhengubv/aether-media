// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Scripting;

namespace AetherMedia.LocalLibrary.Audio.Visualization.Avs;

/// <summary>
/// AVS Super Scope (typecode 0x25). Scripted scope — runs four NS-EEL
/// equation blocks (init, per_frame, per_point, on_beat) and rasterises
/// the resulting (x, y, r, g, b) tuples as a line/dot graph. The script
/// surface mirrors Milkdrop's custom waves; the only AVS-specific binding
/// is <c>v</c> (waveform sample at the current point).
/// </summary>
public sealed class AvsSuperScopeEffect : AvsEffect
{
    private const int VarN     = 0;
    private const int VarI     = 1;
    private const int VarB     = 2;
    private const int VarX     = 3;
    private const int VarY     = 4;
    private const int VarR     = 5;
    private const int VarG     = 6;
    private const int VarBlue  = 7;
    private const int VarV     = 8;
    private const int VarT     = 9;
    private const int Total    = 10;

    private static readonly string[] Names = ["n", "i", "b", "x", "y", "red", "green", "blue", "v", "t"];

    private readonly List<List<AssignStatement>> _init;
    private readonly List<List<AssignStatement>> _perFrame;
    private readonly List<List<AssignStatement>> _perPoint;
    private readonly List<List<AssignStatement>> _onBeat;
    private readonly double[] _registers = new double[Total];
    private bool _ranInit;

    public AvsSuperScopeEffect(ReadOnlySpan<byte> payload)
    {
        var r = new AvsPayloadReader(payload);
        IsEnabled = r.ReadInt32(1) != 0;
        _ = r.ReadInt32(); // effect type (0=lines, 1=dots, 2=solid)
        var initScript   = r.ReadLengthPrefixedString();
        var pfScript     = r.ReadLengthPrefixedString();
        var ppScript     = r.ReadLengthPrefixedString();
        var beatScript   = r.ReadLengthPrefixedString();
        NumPoints = Math.Clamp(r.ReadInt32(100), 1, 4096);

        _init     = Compile(initScript);
        _perFrame = Compile(pfScript);
        _perPoint = Compile(ppScript);
        _onBeat   = Compile(beatScript);
    }

    public int NumPoints { get; }

    /// <inheritdoc/>
    public override string DisplayName => $"SuperScope (n={NumPoints})";

    /// <inheritdoc/>
    public override void Render(RgbaFrame target, AvsRenderContext context, in VisualizationInputs inputs)
    {
        if (!IsEnabled) return;
        if (!_ranInit)
        {
            RunBlock(_init);
            _ranInit = true;
        }

        _registers[VarN] = NumPoints;
        _registers[VarT] = context.TimeSeconds;
        _registers[VarB] = context.OnBeat ? 1 : 0;

        if (context.OnBeat) RunBlock(_onBeat);
        RunBlock(_perFrame);

        var samples = inputs.TimeDomainSamples.Span;
        var channels = Math.Max(1, inputs.Channels);
        var monoLen = samples.Length / channels;

        var w = target.Width;
        var h = target.Height;
        int prevX = -1, prevY = -1;
        for (var i = 0; i < NumPoints; i++)
        {
            _registers[VarI] = (double)i / Math.Max(1, NumPoints - 1);
            if (monoLen > 0)
            {
                var srcIdx = (int)(_registers[VarI] * (monoLen - 1));
                _registers[VarV] = samples[srcIdx * channels];
            }
            else
            {
                _registers[VarV] = 0;
            }

            RunBlock(_perPoint);

            var x = (int)((_registers[VarX] * 0.5 + 0.5) * w);
            var y = (int)((1 - (_registers[VarY] * 0.5 + 0.5)) * h);
            var rr = (byte)Math.Clamp(_registers[VarR] * 255, 0, 255);
            var rg = (byte)Math.Clamp(_registers[VarG] * 255, 0, 255);
            var rb = (byte)Math.Clamp(_registers[VarBlue] * 255, 0, 255);

            if (prevX >= 0)
                DrawLine(target, prevX, prevY, x, y, rr, rg, rb);
            else if (x >= 0 && x < w && y >= 0 && y < h)
                Plot(target, x, y, rr, rg, rb);
            prevX = x;
            prevY = y;
        }
    }

    private void RunBlock(List<List<AssignStatement>> block)
    {
        foreach (var line in block)
            foreach (var step in line)
                _registers[step.TargetVarIndex] = step.Compiled.Evaluate(_registers);
    }

    private static void DrawLine(RgbaFrame f, int x0, int y0, int x1, int y1, byte r, byte g, byte b)
    {
        var dx = Math.Abs(x1 - x0);
        var dy = Math.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var err = dx - dy;
        while (true)
        {
            if (x0 >= 0 && x0 < f.Width && y0 >= 0 && y0 < f.Height) Plot(f, x0, y0, r, g, b);
            if (x0 == x1 && y0 == y1) break;
            var e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
    }

    private static void Plot(RgbaFrame f, int x, int y, byte r, byte g, byte b)
    {
        var i = (y * f.Width + x) * 4;
        f.Pixels[i]     = (byte)Math.Min(255, f.Pixels[i]     + r);
        f.Pixels[i + 1] = (byte)Math.Min(255, f.Pixels[i + 1] + g);
        f.Pixels[i + 2] = (byte)Math.Min(255, f.Pixels[i + 2] + b);
        f.Pixels[i + 3] = 0xFF;
    }

    private static List<List<AssignStatement>> Compile(string source)
    {
        var lines = new List<List<AssignStatement>>();
        if (string.IsNullOrWhiteSpace(source)) return lines;
        foreach (var raw in source.Split('\n'))
        {
            var per = new List<AssignStatement>();
            foreach (var stmt in raw.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                var eq = stmt.IndexOf('=');
                if (eq <= 0) continue;
                var lhs = stmt[..eq].Trim();
                var rhs = stmt[(eq + 1)..].Trim();
                var idx = NameToIndex(lhs);
                if (idx < 0) continue;
                IExpressionScript compiled;
                try { compiled = ExpressionScriptCompiler.Compile(rhs, Names); }
                catch (FormatException) { continue; }
                per.Add(new AssignStatement(idx, compiled));
            }
            lines.Add(per);
        }
        return lines;
    }

    private static int NameToIndex(string name)
    {
        for (var i = 0; i < Names.Length; i++)
            if (string.Equals(Names[i], name, StringComparison.OrdinalIgnoreCase))
                return i;
        return -1;
    }

    private readonly record struct AssignStatement(int TargetVarIndex, IExpressionScript Compiled);
}
