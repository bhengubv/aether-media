// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Scripting;

namespace AetherMedia.LocalLibrary.Audio.Visualization.Milkdrop;

/// <summary>
/// Runs a <see cref="MilkdropPreset"/>'s per_frame AND per_pixel equation
/// blocks. Maintains the live values of <c>zoom / rot / cx / cy / dx / dy /
/// sx / sy / warp / decay</c> plus the q1..q32 inter-equation registers
/// across frames, exactly like Milkdrop's ns-eel2 runtime.
///
/// <para>
/// Per_frame runs once per frame and updates <see cref="State"/>.
/// Per_pixel runs once per warp-mesh vertex (typically 32×24 = 768 verts)
/// per frame, taking the per-vertex (<c>rad</c>, <c>ang</c>, <c>x</c>, <c>y</c>)
/// bindings on top of the post-per_frame state, and produces a per-vertex
/// <see cref="MilkdropFrameState"/>.
/// </para>
///
/// <para>
/// Equations are compiled by <see cref="ExpressionScriptCompiler"/> so eval
/// runs at native JIT speed — no per-frame string parsing.
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
    // Per-pixel bindings.
    private const int VarRad      = VarQ1 + QCount;
    private const int VarAng      = VarRad + 1;
    private const int VarX        = VarAng + 1;
    private const int VarY        = VarX + 1;
    private const int TotalVars   = VarY + 1;

    private static readonly string[] VariableNames = BuildVariableNames();

    private readonly MilkdropPreset _preset;
    private readonly MilkdropParameters _parameters;
    private readonly List<List<AssignStatement>> _perFrameSteps;
    private readonly List<List<AssignStatement>> _perPixelSteps;

    // Live state across frames.
    private readonly double[] _q = new double[QCount];

    public MilkdropEvaluator(MilkdropPreset preset)
    {
        _preset = preset ?? throw new ArgumentNullException(nameof(preset));
        _parameters = new MilkdropParameters(preset.Parameters);
        _perFrameSteps = Compile(preset.PerFrameEquations);
        _perPixelSteps = Compile(preset.PerPixelEquations);

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

    /// <summary>True if the preset declared any per_pixel equations.</summary>
    public bool HasPerPixel => _perPixelSteps.Count > 0;

    /// <summary>
    /// Evaluate the preset's per_frame equations for the next frame.
    /// </summary>
    public void EvaluateFrame(
        double timeSeconds, long frameIndex, double fps,
        double bass, double mid, double treb,
        double progress = 0)
    {
        var values = new double[TotalVars];
        SeedAudio(values, timeSeconds, frameIndex, fps, bass, mid, treb, progress);

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

    /// <summary>
    /// Evaluate per_pixel equations for one warp-mesh vertex.
    /// </summary>
    /// <param name="rad">Distance from centre (0 at centre, ~0.7 at corner).</param>
    /// <param name="ang">Angle from centre in radians.</param>
    /// <param name="x">Normalised x position (0..1).</param>
    /// <param name="y">Normalised y position (0..1).</param>
    /// <returns>The post-per_pixel state for this vertex.</returns>
    public MilkdropFrameState EvaluatePerPixel(double rad, double ang, double x, double y)
    {
        if (_perPixelSteps.Count == 0) return State;

        var values = new double[TotalVars];
        // Audio + frame inputs are constant across the mesh — pre-bind from current state.
        values[VarBass]    = 0; values[VarMid] = 0; values[VarTreb] = 0;
        values[VarBassAtt] = 0; values[VarMidAtt] = 0; values[VarTrebAtt] = 0;

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

        for (var i = 0; i < QCount; i++) values[VarQ1 + i] = _q[i];

        values[VarRad] = rad;
        values[VarAng] = ang;
        values[VarX]   = x;
        values[VarY]   = y;

        foreach (var line in _perPixelSteps)
            foreach (var step in line)
                values[step.TargetVarIndex] = step.Compiled.Evaluate(values);

        return new MilkdropFrameState
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
    }

    /// <summary>Read a q register (q1..q32). One-based indexing.</summary>
    public double GetQ(int oneBased) => _q[oneBased - 1];

    private static void SeedAudio(
        double[] values, double timeSeconds, long frameIndex, double fps,
        double bass, double mid, double treb, double progress)
    {
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
        names[VarRad] = "rad";
        names[VarAng] = "ang";
        names[VarX]   = "x";
        names[VarY]   = "y";
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
