// SPDX-License-Identifier: MIT

using System.Linq.Expressions;

namespace AetherMedia.LocalLibrary.Audio.Scripting;

/// <summary>
/// Compile a math expression string into a delegate using
/// <see cref="System.Linq.Expressions"/>. The .NET JIT does the rest: the
/// returned <see cref="IExpressionScript"/> runs at full native speed, the
/// same way Winamp's ns-eel2 did with its hand-written x86 codegen — only we
/// don't have to maintain an assembler.
///
/// <para>
/// Supported syntax: numeric literals, the variables you supply, parentheses,
/// the operators <c>+ - * / ^</c>, and the functions <c>sin cos tan abs sqrt
/// log exp min max pow</c>.
/// </para>
/// </summary>
public static class ExpressionScriptCompiler
{
    /// <summary>
    /// Compile <paramref name="expression"/>, binding <paramref name="variableNames"/>
    /// to positional values in the resulting <see cref="IExpressionScript"/>.
    /// </summary>
    public static IExpressionScript Compile(string expression, IReadOnlyList<string> variableNames)
    {
        ArgumentException.ThrowIfNullOrEmpty(expression);
        ArgumentNullException.ThrowIfNull(variableNames);

        var arrayParam = Expression.Parameter(typeof(double[]), "values");
        var parser = new Parser(expression, variableNames, arrayParam);
        var body = parser.ParseExpression();
        parser.ExpectEnd();

        var lambda = Expression.Lambda<Func<double[], double>>(body, arrayParam);
        var fn = lambda.Compile();
        return new CompiledScript(variableNames, fn);
    }

    private sealed class CompiledScript(
        IReadOnlyList<string> inputs,
        Func<double[], double> fn) : IExpressionScript
    {
        public IReadOnlyList<string> Inputs { get; } = inputs;

        public double Evaluate(ReadOnlySpan<double> values)
        {
            if (values.Length < Inputs.Count)
                throw new ArgumentException(
                    $"Script expects {Inputs.Count} values; got {values.Length}.", nameof(values));
            // Copy span to array — Linq.Expressions can't take a ref-struct
            // parameter. Future optimisation: pooled array.
            var arr = new double[Inputs.Count];
            for (var i = 0; i < arr.Length; i++) arr[i] = values[i];
            return fn(arr);
        }
    }

    // ── Recursive-descent parser ────────────────────────────────────────────
    private sealed class Parser
    {
        private readonly string _src;
        private readonly IReadOnlyList<string> _variableNames;
        private readonly ParameterExpression _arrayParam;
        private int _pos;

        public Parser(string source, IReadOnlyList<string> variableNames, ParameterExpression arrayParam)
        {
            _src = source;
            _variableNames = variableNames;
            _arrayParam = arrayParam;
        }

        public Expression ParseExpression() => ParseAddSub();

        public void ExpectEnd()
        {
            SkipWhitespace();
            if (_pos != _src.Length)
                throw new FormatException($"Unexpected trailing input at position {_pos}.");
        }

        private Expression ParseAddSub()
        {
            var left = ParseMulDiv();
            while (true)
            {
                SkipWhitespace();
                if (Peek('+')) { _pos++; left = Expression.Add(left, ParseMulDiv()); }
                else if (Peek('-')) { _pos++; left = Expression.Subtract(left, ParseMulDiv()); }
                else break;
            }
            return left;
        }

        private Expression ParseMulDiv()
        {
            var left = ParsePower();
            while (true)
            {
                SkipWhitespace();
                if (Peek('*')) { _pos++; left = Expression.Multiply(left, ParsePower()); }
                else if (Peek('/')) { _pos++; left = Expression.Divide(left, ParsePower()); }
                else break;
            }
            return left;
        }

        private Expression ParsePower()
        {
            var left = ParseUnary();
            SkipWhitespace();
            if (Peek('^'))
            {
                _pos++;
                var right = ParsePower(); // right-assoc
                return Expression.Call(
                    typeof(Math).GetMethod(nameof(Math.Pow), [typeof(double), typeof(double)])!,
                    left, right);
            }
            return left;
        }

        private Expression ParseUnary()
        {
            SkipWhitespace();
            if (Peek('+')) { _pos++; return ParseUnary(); }
            if (Peek('-')) { _pos++; return Expression.Negate(ParseUnary()); }
            return ParseAtom();
        }

        private Expression ParseAtom()
        {
            SkipWhitespace();
            if (_pos >= _src.Length)
                throw new FormatException($"Unexpected end of expression at position {_pos}.");

            if (Peek('('))
            {
                _pos++;
                var inner = ParseAddSub();
                SkipWhitespace();
                if (!Peek(')'))
                    throw new FormatException($"Expected ')' at position {_pos}.");
                _pos++;
                return inner;
            }

            var c = _src[_pos];
            if (char.IsDigit(c) || c == '.') return ParseNumber();
            if (char.IsLetter(c) || c == '_') return ParseIdentifierOrCall();
            throw new FormatException($"Unexpected character '{c}' at position {_pos}.");
        }

        private Expression ParseNumber()
        {
            var start = _pos;
            while (_pos < _src.Length && (char.IsDigit(_src[_pos]) || _src[_pos] == '.'))
                _pos++;
            var text = _src[start.._pos];
            return Expression.Constant(double.Parse(text, System.Globalization.CultureInfo.InvariantCulture));
        }

        private Expression ParseIdentifierOrCall()
        {
            var start = _pos;
            while (_pos < _src.Length && (char.IsLetterOrDigit(_src[_pos]) || _src[_pos] == '_'))
                _pos++;
            var name = _src[start.._pos];

            SkipWhitespace();
            if (Peek('('))
            {
                _pos++;
                var args = new List<Expression>();
                SkipWhitespace();
                if (!Peek(')'))
                {
                    args.Add(ParseAddSub());
                    SkipWhitespace();
                    while (Peek(','))
                    {
                        _pos++;
                        args.Add(ParseAddSub());
                        SkipWhitespace();
                    }
                }
                if (!Peek(')'))
                    throw new FormatException($"Expected ')' at position {_pos}.");
                _pos++;
                return BuildFunctionCall(name, args);
            }

            // Identifier — must match a variable name; bind to values[idx]
            for (var i = 0; i < _variableNames.Count; i++)
                if (_variableNames[i] == name)
                {
                    return Expression.ArrayIndex(_arrayParam, Expression.Constant(i));
                }

            throw new FormatException($"Unknown identifier '{name}' at position {start}.");
        }

        private static Expression BuildFunctionCall(string name, List<Expression> args)
        {
            var t = typeof(Math);
            return name switch
            {
                "sin"  => Expression.Call(t.GetMethod(nameof(Math.Sin),  [typeof(double)])!, args),
                "cos"  => Expression.Call(t.GetMethod(nameof(Math.Cos),  [typeof(double)])!, args),
                "tan"  => Expression.Call(t.GetMethod(nameof(Math.Tan),  [typeof(double)])!, args),
                "abs"  => Expression.Call(t.GetMethod(nameof(Math.Abs),  [typeof(double)])!, args),
                "sqrt" => Expression.Call(t.GetMethod(nameof(Math.Sqrt), [typeof(double)])!, args),
                "log"  => Expression.Call(t.GetMethod(nameof(Math.Log),  [typeof(double)])!, args),
                "exp"  => Expression.Call(t.GetMethod(nameof(Math.Exp),  [typeof(double)])!, args),
                "min"  => Expression.Call(t.GetMethod(nameof(Math.Min),  [typeof(double), typeof(double)])!, args),
                "max"  => Expression.Call(t.GetMethod(nameof(Math.Max),  [typeof(double), typeof(double)])!, args),
                "pow"  => Expression.Call(t.GetMethod(nameof(Math.Pow),  [typeof(double), typeof(double)])!, args),
                _ => throw new FormatException($"Unknown function '{name}'."),
            };
        }

        private bool Peek(char c) => _pos < _src.Length && _src[_pos] == c;
        private void SkipWhitespace()
        {
            while (_pos < _src.Length && char.IsWhiteSpace(_src[_pos])) _pos++;
        }
    }
}
