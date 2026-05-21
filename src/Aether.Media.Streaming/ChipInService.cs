// SPDX-License-Identifier: MIT
using System.Collections.Concurrent;

namespace Aether.Media.Streaming;

/// <summary>
/// Models a ChipIn session where viewers contribute to a creator's pool.
/// Maps to IWatchTogetherService.StartChipInAsync / ContributeAsync.
/// </summary>
public sealed class ChipInSession
{
    public string  SessionId    { get; init; } = Guid.NewGuid().ToString();
    public string  ContentHash  { get; init; } = "";
    public string  CreatorUhid  { get; init; } = "";
    public decimal TargetAmount { get; init; }
    public string  Currency     { get; init; } = "ZAR";
    public decimal TotalRaised  { get; private set; }
    public bool    IsComplete   => TotalRaised >= TargetAmount;
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;

    private readonly List<ChipInContribution> _contributions = [];
    public IReadOnlyList<ChipInContribution> Contributions => _contributions;

    public void Contribute(string fromUhid, decimal amount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fromUhid);
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
        _contributions.Add(new ChipInContribution(fromUhid, amount, DateTimeOffset.UtcNow));
        TotalRaised += amount;
    }
}

public sealed record ChipInContribution(string FromUhid, decimal Amount, DateTimeOffset ContributedAt);

/// <summary>Manages active ChipIn sessions for the local node.</summary>
public sealed class ChipInManager
{
    private readonly ConcurrentDictionary<string, ChipInSession> _sessions = new();

    public ChipInSession StartSession(string contentHash, string creatorUhid, decimal target, string currency = "ZAR")
    {
        var session = new ChipInSession
        {
            ContentHash  = contentHash,
            CreatorUhid  = creatorUhid,
            TargetAmount = target,
            Currency     = currency,
        };
        _sessions[session.SessionId] = session;
        return session;
    }

    public bool Contribute(string sessionId, string fromUhid, decimal amount)
    {
        if (!_sessions.TryGetValue(sessionId, out var session)) return false;
        session.Contribute(fromUhid, amount);
        return true;
    }

    public ChipInSession? GetSession(string sessionId)
        => _sessions.TryGetValue(sessionId, out var s) ? s : null;

    public IReadOnlyCollection<ChipInSession> ActiveSessions
        => _sessions.Values.Where(s => !s.IsComplete).ToList();
}
