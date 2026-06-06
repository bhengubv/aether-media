// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Scripting;

namespace AetherMedia.LocalLibrary.Audio.Visualization.Milkdrop;

/// <summary>
/// Per-wave NS-EEL evaluator. Compiles the wave's init (once),
/// per_frame (once per frame), and per_point (once per output sample)
/// equation blocks. Per_point sees <c>sample</c> (0..1), <c>value1</c> +
/// <c>value2</c> (audio samples for the L / R channel), and the wave's
/// mutable output state (<c>x / y / r / g / b / a</c>).
/// </summary>
public sealed class MilkdropWaveEvaluator
{
    private const int VarT1 = 0;
    private const int TCount = 8;
    private const int VarQ1 = VarT1 + TCount;
    private const int QCount = 32;

    private const int VarBass     = VarQ1 + QCount;
    private const int VarMid      = VarBass + 1;
    private const int VarTreb     = VarMid + 1;
    private const int VarTime     = VarTreb + 1;
    private const int VarFrame    = VarTime + 1;
    private const int VarFps      = VarFrame + 1;
    private const int VarSample   = VarFps + 1;
    private const int VarValue1   = VarSample + 1;
    private const int VarValue2   = VarValue1 + 1;
    private const int VarX        = VarValue2 + 1;
    private const int VarY        = VarX + 1;
    private const int VarR        = VarY + 1;
    private const int VarG        = VarR + 1;
    private const int VarB        = VarG + 1;
    private const int VarA        = VarB + 1;
    private const int TotalVars   = VarA + 1;

    private static readonly string[] VariableNames = BuildVariableNames();

    private readonly MilkdropCustomWave _wave;
    private readonly List<List<AssignStatement>> _perFrameSteps;
    private readonly List<List<AssignStatement>> _perPointSteps;
    private readonly double[] _t = new double[TCount];

    public MilkdropWaveEvaluator(MilkdropCustomWave wave)
    {
        _wave = wave ?? throw new ArgumentNullException(nameof(wave));

        // Run init equations to populate t1..t8.
        var initSteps = Compile(wave.InitEquations);
        if (initSteps.Count > 0)
        {
            var v = new double[TotalVars];
            foreach (var line in initSteps)
                foreach (var step in line) v[step.TargetVarIndex] = step.Compiled.Evaluate(v);
            for (var i = 0; i < TCount; i++) _t[i] = v[VarT1 + i];
        }

        _perFrameSteps = Compile(wave.PerFrameEquations);
        _perPointSteps = Compile(wave.PerPointEquations);
    }

    /// <summary>
    /// Compute every output point. Runs per_frame once, then per_point for
    /// each sample, returning a list of points with their colours.
    /// </summary>
    public IReadOnlyList<MilkdropWavePoint> EvaluateFrame(
        double timeSeconds, long frameIndex, double fps,
        double bass, double mid, double treb,
        ReadOnlySpan<double> qRegisters,
        ReadOnlySpan<float> channelL,
        ReadOnlySpan<float> channelR)
    {
        var values = new double[TotalVars];
        for (var i = 0; i < TCount; i++) values[VarT1 + i] = _t[i];
        for (var i = 0; i < QCount && i < qRegisters.Length; i++) values[VarQ1 + i] = qRegisters[i];
        values[VarBass]  = bass;  values[VarMid] = mid; values[VarTreb] = treb;
        values[VarTime]  = timeSeconds; values[VarFrame] = frameIndex; values[VarFps] = fps;
        values[VarR] = _wave.R; values[VarG] = _wave.G; values[VarB] = _wave.B; values[VarA] = _wave.A;

        // per_frame runs once for the whole wave.
        foreach (var line in _perFrameSteps)
            foreach (var step in line) values[step.TargetVarIndex] = step.Compiled.Evaluate(values);

        var n = _wave.Samples;
        var srcLen = Math.Min(channelL.Length, channelR.Length);
        var points = new List<MilkdropWavePoint>(n);
        for (var i = 0; i < n; i++)
        {
            var sampleT = n > 1 ? (double)i / (n - 1) : 0.0;
            values[VarSample] = sampleT;
            if (srcLen > 0)
            {
                var srcIdx = (int)(sampleT * (srcLen - 1));
                values[VarValue1] = channelL[srcIdx];
                values[VarValue2] = channelR[srcIdx];
            }
            // Reset x/y to the bottom-centre of the screen at the start of every point.
            values[VarX] = 0.5;
            values[VarY] = 0.5;

            foreach (var line in _perPointSteps)
                foreach (var step in line) values[step.TargetVarIndex] = step.Compiled.Evaluate(values);

            points.Add(new MilkdropWavePoint(
                X: values[VarX], Y: values[VarY],
                Rgba: ((float)values[VarR], (float)values[VarG], (float)values[VarB], (float)values[VarA])));
        }

        for (var i = 0; i < TCount; i++) _t[i] = values[VarT1 + i];
        return points;
    }

    /// <summary>Convenience: read-only view of the source wave definition.</summary>
    public MilkdropCustomWave Wave => _wave;

    private static List<List<AssignStatement>> Compile(IReadOnlyList<string> equationLines)
    {
        var lines = new List<List<AssignStatement>>();
        foreach (var raw in equationLines)
        {
            var perLine = new List<AssignStatement>();
            foreach (var stmt in raw.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                var eq = stmt.IndexOf('=');
                if (eq <= 0) continue;
                var lhs = stmt[..eq].Trim();
                var rhs = stmt[(eq + 1)..].Trim();
                var idx = NameToIndex(lhs);
                if (idx < 0) continue;
                IExpressionScript compiled;
                try { compiled = ExpressionScriptCompiler.Compile(rhs, VariableNames); }
                catch (FormatException) { continue; }
                perLine.Add(new AssignStatement(idx, compiled));
            }
            lines.Add(perLine);
        }
        return lines;
    }

    private static int NameToIndex(string name)
    {
        for (var i = 0; i < VariableNames.Length; i++)
            if (string.Equals(VariableNames[i], name, StringComparison.OrdinalIgnoreCase))
                return i;
        return -1;
    }

    private static string[] BuildVariableNames()
    {
        var names = new string[TotalVars];
        for (var i = 0; i < TCount; i++) names[VarT1 + i] = "t" + (i + 1);
        for (var i = 0; i < QCount; i++) names[VarQ1 + i] = "q" + (i + 1);
        names[VarBass] = "bass"; names[VarMid] = "mid"; names[VarTreb] = "treb";
        names[VarTime] = "time"; names[VarFrame] = "frame"; names[VarFps] = "fps";
        names[VarSample] = "sample"; names[VarValue1] = "value1"; names[VarValue2] = "value2";
        names[VarX] = "x"; names[VarY] = "y";
        names[VarR] = "r"; names[VarG] = "g"; names[VarB] = "b"; names[VarA] = "a";
        return names;
    }

    private readonly record struct AssignStatement(int TargetVarIndex, IExpressionScript Compiled);
}

/// <summary>One point on a custom wave's polyline.</summary>
public sealed record MilkdropWavePoint(
    double X,
    double Y,
    (float R, float G, float B, float A) Rgba);
