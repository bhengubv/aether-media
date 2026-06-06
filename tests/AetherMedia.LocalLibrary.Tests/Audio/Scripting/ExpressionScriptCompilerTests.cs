// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Scripting;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Scripting;

public class ExpressionScriptCompilerTests
{
    [Fact]
    public void Constant_Expression_Compiles()
    {
        var s = ExpressionScriptCompiler.Compile("3.14", []);
        Assert.Equal(3.14, s.Evaluate([]), 6);
    }

    [Fact]
    public void Arithmetic_Precedence()
    {
        var s = ExpressionScriptCompiler.Compile("1 + 2 * 3", []);
        Assert.Equal(7, s.Evaluate([]), 6);
    }

    [Fact]
    public void Power_RightAssociative()
    {
        var s = ExpressionScriptCompiler.Compile("2 ^ 3 ^ 2", []);
        Assert.Equal(512, s.Evaluate([]), 6); // 2 ^ (3 ^ 2) = 2 ^ 9
    }

    [Fact]
    public void Variables_BoundByName()
    {
        var s = ExpressionScriptCompiler.Compile("x * 2 + y", ["x", "y"]);
        Assert.Equal(7, s.Evaluate([2, 3]), 6);
    }

    [Fact]
    public void MathFunctions_AreAvailable()
    {
        var s = ExpressionScriptCompiler.Compile("sin(0) + cos(0) + sqrt(16)", []);
        Assert.Equal(5, s.Evaluate([]), 6);
    }

    [Fact]
    public void UnknownIdentifier_Throws()
    {
        Assert.Throws<FormatException>(
            () => ExpressionScriptCompiler.Compile("x + y", ["x"]));
    }

    [Fact]
    public void NegativeAndParens()
    {
        var s = ExpressionScriptCompiler.Compile("-(1 + 2) * 4", []);
        Assert.Equal(-12, s.Evaluate([]), 6);
    }

    [Fact]
    public void WrongArgCount_Throws()
    {
        var s = ExpressionScriptCompiler.Compile("x", ["x"]);
        Assert.Throws<ArgumentException>(() => s.Evaluate([]));
    }
}
