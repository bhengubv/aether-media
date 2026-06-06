// SPDX-License-Identifier: MIT
using AetherNet.Forge.Core;

namespace AetherNet.Forge.Tests;

public sealed class PackageIdParserTests
{
    // ── npm ────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Npm_ReturnsCorrectComponents()
    {
        var (ecosystem, name, version) = PackageIdParser.Parse("npm:react@18.2.0");

        Assert.Equal("npm",     ecosystem);
        Assert.Equal("react",   name);
        Assert.Equal("18.2.0",  version);
    }

    [Fact]
    public void Parse_NpmScoped_ReturnsCorrectComponents()
    {
        var (ecosystem, name, version) = PackageIdParser.Parse("npm:@types/node@20.0.0");

        Assert.Equal("npm",         ecosystem);
        Assert.Equal("@types/node", name);
        Assert.Equal("20.0.0",      version);
    }

    // ── git ────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Git_ReturnsCorrectComponents()
    {
        var (ecosystem, name, version) =
            PackageIdParser.Parse("git:github.com/user/repo@abc1234");

        Assert.Equal("git",                  ecosystem);
        Assert.Equal("github.com/user/repo", name);
        Assert.Equal("abc1234",              version);
    }

    // ── pip ────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Pip_ReturnsCorrectComponents()
    {
        var (ecosystem, name, version) = PackageIdParser.Parse("pip:requests@2.31.0");

        Assert.Equal("pip",      ecosystem);
        Assert.Equal("requests", name);
        Assert.Equal("2.31.0",   version);
    }

    // ── cargo ──────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Cargo_ReturnsCorrectComponents()
    {
        var (ecosystem, name, version) = PackageIdParser.Parse("cargo:serde@1.0.193");

        Assert.Equal("cargo",   ecosystem);
        Assert.Equal("serde",   name);
        Assert.Equal("1.0.193", version);
    }

    // ── go ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_Go_ReturnsCorrectComponents()
    {
        var (ecosystem, name, version) =
            PackageIdParser.Parse("go:github.com/gin-gonic/gin@v1.9.1");

        Assert.Equal("go",                       ecosystem);
        Assert.Equal("github.com/gin-gonic/gin", name);
        Assert.Equal("v1.9.1",                   version);
    }

    // ── error cases ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyOrWhitespace_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => PackageIdParser.Parse(value));
    }

    [Fact]
    public void Parse_MissingColon_Throws()
    {
        Assert.Throws<ArgumentException>(() => PackageIdParser.Parse("npmreact@18.2.0"));
    }

    [Fact]
    public void Parse_UnknownEcosystem_Throws()
    {
        Assert.Throws<NotSupportedException>(() => PackageIdParser.Parse("nuget:Newtonsoft.Json@13.0.3"));
    }

    [Fact]
    public void Parse_MissingVersion_Throws()
    {
        Assert.Throws<ArgumentException>(() => PackageIdParser.Parse("npm:react"));
    }

    [Fact]
    public void Parse_EmptyVersion_Throws()
    {
        Assert.Throws<ArgumentException>(() => PackageIdParser.Parse("npm:react@"));
    }

    [Fact]
    public void Parse_EcosystemCaseInsensitive()
    {
        var (ecosystem, _, _) = PackageIdParser.Parse("NPM:react@18.2.0");
        Assert.Equal("npm", ecosystem);
    }
}
