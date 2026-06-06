// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Scripting;

/// <summary>
/// A compiled, callable math expression — the managed equivalent of Winamp's
/// ns-eel2 JIT. <see cref="ExpressionScriptCompiler"/> parses a source string
/// like <c>"sin(x * 2 * 3.14159 * 0.05) + amp"</c> and returns one of these
/// you can call millions of times per second.
/// </summary>
public interface IExpressionScript
{
    /// <summary>Variable names this script reads.</summary>
    IReadOnlyList<string> Inputs { get; }

    /// <summary>Evaluate with the variable values in the same order as <see cref="Inputs"/>.</summary>
    double Evaluate(ReadOnlySpan<double> values);
}
