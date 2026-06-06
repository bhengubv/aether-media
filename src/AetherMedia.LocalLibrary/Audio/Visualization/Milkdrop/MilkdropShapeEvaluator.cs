// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Scripting;

namespace AetherMedia.LocalLibrary.Audio.Visualization.Milkdrop;

/// <summary>
/// Per-shape NS-EEL evaluator. Compiles + runs a
/// <see cref="MilkdropCustomShape"/>'s init equations once at construction
/// and its per_frame equations every render frame (once per instance).
///
/// <para>
/// Variable layout: t1..t8 (per-shape persistent registers) + q1..q32
/// (carried in from <see cref="MilkdropEvaluator"/>) + audio + time/frame/
/// fps + <c>instance</c> + the shape's mutable output state
/// (<c>x / y / rad / ang / r / g / b / a / r2 / g2 / b2 / a2</c>).
/// </para>
/// </summary>
public sealed class MilkdropShapeEvaluator
{
    private const int VarT1 = 0;
    private const int TCount = 8;
    private const int VarQ1 = VarT1 + TCount;
    private const int QCount = 32;

    private const int VarBass = VarQ1 + QCount;
    private const int VarMid       = VarBass + 1;
    private const int VarTreb      = VarMid + 1;
    private const int VarTime      = VarTreb + 1;
    private const int VarFrame     = VarTime + 1;
    private const int VarFps       = VarFrame + 1;
    private const int VarInstance  = VarFps + 1;
    private const int VarNumInst   = VarInstance + 1;
    private const int VarX         = VarNumInst + 1;
    private const int VarY         = VarX + 1;
    private const int VarRad       = VarY + 1;
    private const int VarAng       = VarRad + 1;
    private const int VarSides     = VarAng + 1;
    private const int VarR         = VarSides + 1;
    private const int VarG         = VarR + 1;
    private const int VarB         = VarG + 1;
    private const int VarA         = VarB + 1;
    private const int VarR2        = VarA + 1;
    private const int VarG2        = VarR2 + 1;
    private const int VarB2        = VarG2 + 1;
    private const int VarA2        = VarB2 + 1;
    private const int VarBorderR   = VarA2 + 1;
    private const int VarBorderG   = VarBorderR + 1;
    private const int VarBorderB   = VarBorderG + 1;
    private const int VarBorderA   = VarBorderB + 1;
    private const int TotalVars    = VarBorderA + 1;

    private static readonly string[] VariableNames = BuildVariableNames();

    private readonly MilkdropCustomShape _shape;
    private readonly List<List<AssignStatement>> _perFrameSteps;
    private readonly double[] _t = new double[TCount];

    public MilkdropShapeEvaluator(MilkdropCustomShape shape)
    {
        _shape = shape ?? throw new ArgumentNullException(nameof(shape));

        // Run init equations once to seed t1..t8 (and any other initial state).
        var initSteps = Compile(shape.InitEquations);
        if (initSteps.Count > 0)
        {
            var initValues = new double[TotalVars];
            SeedInitValues(initValues);
            foreach (var line in initSteps)
                foreach (var step in line)
                    initValues[step.TargetVarIndex] = step.Compiled.Evaluate(initValues);
            for (var i = 0; i < TCount; i++) _t[i] = initValues[VarT1 + i];
        }

        _perFrameSteps = Compile(shape.PerFrameEquations);
    }

    /// <summary>The source shape definition this evaluator wraps.</summary>
    public MilkdropCustomShape Shape => _shape;

    /// <summary>Compute the live state for one instance of this shape.</summary>
    public MilkdropShapeInstance Evaluate(
        int instance,
        double timeSeconds, long frameIndex, double fps,
        double bass, double mid, double treb,
        ReadOnlySpan<double> qRegisters)
    {
        var values = new double[TotalVars];
        for (var i = 0; i < TCount; i++) values[VarT1 + i] = _t[i];
        for (var i = 0; i < QCount && i < qRegisters.Length; i++) values[VarQ1 + i] = qRegisters[i];
        values[VarBass]    = bass;
        values[VarMid]     = mid;
        values[VarTreb]    = treb;
        values[VarTime]    = timeSeconds;
        values[VarFrame]   = frameIndex;
        values[VarFps]     = fps;
        values[VarInstance]= instance;
        values[VarNumInst] = _shape.Instances;
        values[VarX]       = _shape.X;
        values[VarY]       = _shape.Y;
        values[VarRad]     = _shape.Radius;
        values[VarAng]     = _shape.Angle;
        values[VarSides]   = _shape.Sides;
        values[VarR]       = _shape.R;       values[VarG]       = _shape.G;
        values[VarB]       = _shape.B;       values[VarA]       = _shape.A;
        values[VarR2]      = _shape.R2;      values[VarG2]      = _shape.G2;
        values[VarB2]      = _shape.B2;      values[VarA2]      = _shape.A2;
        values[VarBorderR] = _shape.BorderR; values[VarBorderG] = _shape.BorderG;
        values[VarBorderB] = _shape.BorderB; values[VarBorderA] = _shape.BorderA;

        foreach (var line in _perFrameSteps)
            foreach (var step in line)
                values[step.TargetVarIndex] = step.Compiled.Evaluate(values);

        for (var i = 0; i < TCount; i++) _t[i] = values[VarT1 + i];

        return new MilkdropShapeInstance(
            X: values[VarX], Y: values[VarY],
            Radius: values[VarRad], Angle: values[VarAng],
            Sides: Math.Clamp((int)values[VarSides], 3, 100),
            FillRgba: ((float)values[VarR], (float)values[VarG], (float)values[VarB], (float)values[VarA]),
            CenterRgba: ((float)values[VarR2], (float)values[VarG2], (float)values[VarB2], (float)values[VarA2]),
            BorderRgba: ((float)values[VarBorderR], (float)values[VarBorderG], (float)values[VarBorderB], (float)values[VarBorderA]),
            Additive: _shape.Additive,
            ThickOutline: _shape.ThickOutline);
    }

    private void SeedInitValues(double[] values)
    {
        values[VarX]     = _shape.X;     values[VarY]     = _shape.Y;
        values[VarRad]   = _shape.Radius; values[VarAng]   = _shape.Angle;
        values[VarSides] = _shape.Sides;
        values[VarR]     = _shape.R;     values[VarG]     = _shape.G;
        values[VarB]     = _shape.B;     values[VarA]     = _shape.A;
    }

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
        names[VarBass]     = "bass";     names[VarMid]    = "mid";     names[VarTreb]   = "treb";
        names[VarTime]     = "time";     names[VarFrame]  = "frame";   names[VarFps]    = "fps";
        names[VarInstance] = "instance"; names[VarNumInst]= "num_inst";
        names[VarX]        = "x";        names[VarY]      = "y";
        names[VarRad]      = "rad";      names[VarAng]    = "ang";
        names[VarSides]    = "sides";
        names[VarR]        = "r";        names[VarG]      = "g";        names[VarB]      = "b";        names[VarA]      = "a";
        names[VarR2]       = "r2";       names[VarG2]     = "g2";       names[VarB2]     = "b2";       names[VarA2]     = "a2";
        names[VarBorderR]  = "border_r"; names[VarBorderG]= "border_g"; names[VarBorderB]= "border_b"; names[VarBorderA]= "border_a";
        return names;
    }

    private readonly record struct AssignStatement(int TargetVarIndex, IExpressionScript Compiled);
}

/// <summary>One instance of a custom shape, ready for rasterisation.</summary>
public sealed record MilkdropShapeInstance(
    double X,
    double Y,
    double Radius,
    double Angle,
    int Sides,
    (float R, float G, float B, float A) FillRgba,
    (float R, float G, float B, float A) CenterRgba,
    (float R, float G, float B, float A) BorderRgba,
    bool Additive,
    bool ThickOutline);
