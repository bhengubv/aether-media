// SPDX-License-Identifier: MIT

using Aether.Media.AI.Tests.Helpers;

namespace Aether.Media.AI.Tests;

/// <summary>Unit tests for <see cref="CreatorReputationView"/>.</summary>
public sealed class CreatorReputationViewTests
{
    // ── Constructor ────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullReputation_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new CreatorReputationView(null!));

    // ── GetCreatorScoreAsync ───────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetCreatorScore_BlankUhid_Returns1_0(string uhid)
    {
        var rep  = new FakeReputationService();
        var view = new CreatorReputationView(rep);

        var score = await view.GetCreatorScoreAsync(uhid);

        Assert.Equal(1.0, score);
    }

    [Fact]
    public async Task GetCreatorScore_KnownUhid_DelegatesToService()
    {
        var rep = new FakeReputationService();
        rep.Scores["creator-abc"] = 0.75;

        var view  = new CreatorReputationView(rep);
        var score = await view.GetCreatorScoreAsync("creator-abc");

        Assert.Equal(0.75, score, precision: 4);
    }

    [Fact]
    public async Task GetCreatorScore_UnknownUhid_Returns1_0()
    {
        var rep  = new FakeReputationService();
        var view = new CreatorReputationView(rep);

        // No score registered → service returns 1.0 (benefit of the doubt)
        var score = await view.GetCreatorScoreAsync("new-creator");

        Assert.Equal(1.0, score);
    }

    // ── GetTopCreatorsAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetTopCreators_ZeroLimit_ReturnsEmpty()
    {
        var rep  = new FakeReputationService { Scores = { ["creator-a"] = 0.9 } };
        var view = new CreatorReputationView(rep);

        var top = await view.GetTopCreatorsAsync(limit: 0);

        Assert.Empty(top);
    }

    [Fact]
    public async Task GetTopCreators_EmptyService_ReturnsEmpty()
    {
        var rep  = new FakeReputationService();
        var view = new CreatorReputationView(rep);

        var top = await view.GetTopCreatorsAsync(limit: 10);

        Assert.Empty(top);
    }

    [Fact]
    public async Task GetTopCreators_SortedDescendingByScore()
    {
        var rep = new FakeReputationService();
        rep.Scores["c"] = 0.3;
        rep.Scores["a"] = 0.9;
        rep.Scores["b"] = 0.6;

        var view = new CreatorReputationView(rep);
        var top  = await view.GetTopCreatorsAsync(limit: 3);

        Assert.Equal(3, top.Count);
        Assert.Equal("a", top[0].Uhid);
        Assert.Equal("b", top[1].Uhid);
        Assert.Equal("c", top[2].Uhid);
    }

    [Fact]
    public async Task GetTopCreators_LimitApplied()
    {
        var rep = new FakeReputationService();
        rep.Scores["a"] = 0.9;
        rep.Scores["b"] = 0.8;
        rep.Scores["c"] = 0.7;
        rep.Scores["d"] = 0.6;

        var view = new CreatorReputationView(rep);
        var top  = await view.GetTopCreatorsAsync(limit: 2);

        Assert.Equal(2, top.Count);
        Assert.Equal("a", top[0].Uhid);
        Assert.Equal("b", top[1].Uhid);
    }
}
