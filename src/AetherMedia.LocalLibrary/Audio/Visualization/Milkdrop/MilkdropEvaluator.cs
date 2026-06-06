// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Scripting;

namespace AetherMedia.LocalLibrary.Audio.Visualization.Milkdrop;

/// <summary>
/// Runs a <see cref="MilkdropPreset"/>'s per_frame equation block. Maintains
/// the live values of <c>zoom / rot / cx / cy / dx / dy / sx / sy / warp /
/// decay</c> plus the q1..q32 inter-equation registers across frames, the
/// way Milkdrop's ns-eel2 runtime did.
///
/// <para>
/// Equations are split on <c>;</c> and compiled by
/// <see cref="ExpressionScriptCompiler"/> — that gets us native-speed
/// evaluation without writing an assembler. Single-equation strings without
/// a trailing semicolon are accepted; commented-out lines (<c>//…</c>) are
/// stripped at parse time.
/// </para>
/// </summary>
public sealed class MilkdropEvaluator
{
    // ── Variable index layout (positional inputs to the compiled scripts) ──
    private const int VarTime     = 0;
    private const int VarFrame    = 1;
    private const int VarFps      = 2;
    private const int VarBass     = 3;
    private const int VarMid      = 4;
    private const int VarTreb     = 5;
    private const int VarBassAtt  = 6;
    private const int VarMidAtt   = 7;
    private const int VarTrebAtt  = 8;
    private const int VarProgress = 9;
    private const int VarZoom     = 10;
    private const int VarRot      = 11;
    private const int VarCx       = 12;
    private const int VarCy       = 13;
    private const int VarDx       = 14;
    private const int VarDy       = 15;
    private const int VarSx       = 16;
    private const int VarSy       = 17;
    private const int VarWarp     = 18;
    private const int VarDecay    = 19;
    private const int VarQ1       = 20;
    private const int QCount      = 32;
    private const int TotalVars   = VarQ1 + QCount;

    private static readonly string[] VariableNames = BuildVariableNames();

    private readonly MilkdropPreset _preset;
    private readonly MilkdropParameters _parameters;
    private readonly List<List<AssignStatement>> _perFrameSteps;

    // Live state across frames.
    private readonly double[] _q = new double[QCount];

    public MilkdropEvaluator(MilkdropPreset preset)
    {
        _preset = preset ?? throw new ArgumentNullException(nameof(preset));
        _parameters = new MilkdropParameters(preset.Parameters);
        _perFrameSteps = Compile(preset.PerFrameEquations);

        // Initialise zoom/rot/etc with the preset start values.
        State = new MilkdropFrameState
        {
            Zoom = _parameters.Zoom,
            Rot = _parameters.Rot,
            Cx = _parameters.Cx,
            Cy = _parameters.Cy,
            Dx = _parameters.Dx,
            Dy = _parameters.Dy,
            Sx = _parameters.Sx,
            Sy = _parameters.Sy,
            Warp = _parameters.Warp,
            Decay = _parameters.Decay,
        };
    }

    /// <summary>Typed accessor over the preset's parameter bag.</summary>
    public MilkdropParameters Parameters => _parameters;

    /// <summary>The frame state — updated each call to <see cref="EvaluateFrame"/>.</summary>
    public MilkdropFrameState State { get; private set; }

    /// <summary>
    /// Evaluate the preset's per_frame equations for the next frame.
    /// </summary>
    /// <param name="timeSeconds">Wall-clock seconds since preset start.</param>
    /// <param name="frameIndex">Frame counter.</param>
    /// <param name="fps">Render rate, used by some presets.</param>
    /// <param name="bass">Bass band energy 0..N (Milkdrop nominal range ~0..3).</param>
    /// <param name="mid">Mid band energy.</param>
    /// <param name="treb">Treble band energy.</param>
    /// <param name="progress">Preset progress 0..1 if the host is doing scheduled blends.</param>
    public void EvaluateFrame(
        double timeSeconds, long frameIndex, double fps,
        double bass, double mid, double treb,
        double progress = 0)
    {
        var values = new double[TotalVars];
        values[VarTime]     = timeSeconds;
        values[VarFrame]    = frameIndex;
        values[VarFps]      = fps;
        values[VarBass]     = bass;
        values[VarMid]      = mid;
        values[VarTreb]     = treb;
        // Attenuated variants — Milkdrop uses a low-pass filter; without
        // multi-frame smoothing here we hand back the same energy.
        values[VarBassAtt]  = bass;
        values[VarMidAtt]   = mid;
        values[VarTrebAtt]  = treb;
        values[VarProgress] = progress;

        // Seed the dynamics with the live state from last frame.
        values[VarZoom]  = State.Zoom;
        values[VarRot]   = State.Rot;
        values[VarCx]    = State.Cx;
        values[VarCy]    = State.Cy;
        values[VarDx]    = State.Dx;
        values[VarDy]    = State.Dy;
        values[VarSx]    = State.Sx;
        values[VarSy]    = State.Sy;
        values[VarWarp]  = State.Warp;
        values[VarDecay] = State.Decay;

        // Carry q registers across frames.
        for (var i = 0; i < QCount; i++) values[VarQ1 + i] = _q[i];

        // Each per_frame line may contain multiple `name = expr` statements
        // joined by `;`. Execute in order, updating `values` between them
        // so later statements can reference earlier results within the same
        // line.
        foreach (var line in _perFrameSteps)
            foreach (var step in line)
                values[step.TargetVarIndex] = step.Compiled.Evaluate(values);

        State = new MilkdropFrameState
        {
            Zoom = values[VarZoom],
            Rot = values[VarRot],
            Cx = values[VarCx],
            Cy = values[VarCy],
            Dx = values[VarDx],
            Dy = values[VarDy],
            Sx = values[VarSx],
            Sy = values[VarSy],
            Warp = values[VarWarp],
            Decay = Math.Clamp(values[VarDecay], 0.0, 1.0),
        };
        for (var i = 0; i < QCount; i++) _q[i] = values[VarQ1 + i];
    }

    /// <summary>Read a q register (q1..q32). One-based indexing.</summary>
    public double GetQ(int oneBased) => _q[oneBased - 1];

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
                if (idx < 0) continue; // assignment to an unknown variable — skip silently
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
        names[VarTime]     = "time";
        names[VarFrame]    = "frame";
        names[VarFps]      = "fps";
        names[VarBass]     = "bass";
        names[VarMid]      = "mid";
        names[VarTreb]     = "treb";
        names[VarBassAtt]  = "bass_att";
        names[VarMidAtt]   = "mid_att";
        names[VarTrebAtt]  = "treb_att";
        names[VarProgress] = "progress";
        names[VarZoom]     = "zoom";
        names[VarRot]      = "rot";
        names[VarCx]       = "cx";
        names[VarCy]       = "cy";
        names[VarDx]       = "dx";
        names[VarDy]       = "dy";
        names[VarSx]       = "sx";
        names[VarSy]       = "sy";
        names[VarWarp]     = "warp";
        names[VarDecay]    = "decay";
        for (var i = 0; i < QCount; i++) names[VarQ1 + i] = "q" + (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        return names;
    }

    private readonly record struct AssignStatement(int TargetVarIndex, IExpressionScript Compiled);
}

/// <summary>Mutable per-frame state. Recomputed each call to <see cref="MilkdropEvaluator.EvaluateFrame"/>.</summary>
public sealed class MilkdropFrameState
{
    public double Zoom  { get; set; } = 1.0;
    public double Rot   { get; set; }
    public double Cx    { get; set; } = 0.5;
    public double Cy    { get; set; } = 0.5;
    public double Dx    { get; set; }
    public double Dy    { get; set; }
    public double Sx    { get; set; } = 1.0;
    public double Sy    { get; set; } = 1.0;
    public double Warp  { get; set; }
    public double Decay { get; set; } = 0.96;
}
