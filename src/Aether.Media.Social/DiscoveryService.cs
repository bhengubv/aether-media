// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;
using Aether.Handshake;
using Aether.Media.Core;
using Aether.Media.Core.Models;
using Aether.Media.Identity;
using Aether.Models;
using Aether.Streaming;
using Aether.Streaming.Models;

namespace Aether.Media.Social;

/// <summary>
/// Discovers nearby content creators by listening to <see cref="IHandshakeService.PeerNegotiated"/>
/// events and checking whether the negotiated peer advertises the
/// <see cref="NodeCapabilities.Streaming"/> capability.
///
/// <para>
/// For each streaming-capable peer the service attempts to resolve a
/// <see cref="MediaProfile"/> via <see cref="IProfileService"/>.  If the profile
/// is not yet cached the discovery entry is still recorded (with a minimal
/// profile) and the <see cref="CreatorDiscovered"/> event is fired so the UI can
/// start reacting immediately.
/// </para>
/// </summary>
public sealed class DiscoveryService : IDiscoveryService
{
    // ── Events ─────────────────────────────────────────────────────────────
    public event EventHandler<MediaProfile>? CreatorDiscovered;

    // ── State ──────────────────────────────────────────────────────────────

    // UHID → MediaProfile of each discovered streaming-capable peer
    private readonly ConcurrentDictionary<string, MediaProfile> _discoveredCreators =
        new(StringComparer.Ordinal);

    // ── Dependencies ───────────────────────────────────────────────────────
    private readonly IHandshakeService _handshake;
    private readonly IStreamingService _streaming;
    private readonly IProfileService _profileService;
    private readonly FootprintGuard? _guard;

    private bool _started;

    public DiscoveryService(
        IHandshakeService handshake,
        IStreamingService streaming,
        IProfileService profileService,
        FootprintGuard? guard = null)
    {
        _handshake      = handshake      ?? throw new ArgumentNullException(nameof(handshake));
        _streaming      = streaming      ?? throw new ArgumentNullException(nameof(streaming));
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _guard          = guard;
    }

    // ── IDiscoveryService ──────────────────────────────────────────────────

    public Task StartAsync(CancellationToken ct = default)
    {
        if (_started) return Task.CompletedTask;
        _started = true;

        _handshake.PeerNegotiated += OnPeerNegotiated;

        // Seed from peers that already completed negotiation before Start was called
        foreach (var caps in _handshake.GetAllNegotiated())
            EnqueueCreatorDiscovery(caps);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        if (!_started) return Task.CompletedTask;
        _started = false;

        _handshake.PeerNegotiated -= OnPeerNegotiated;

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<MediaProfile>> GetNearbyCreatorsAsync(CancellationToken ct = default)
    {
        IReadOnlyList<MediaProfile> snapshot = [.. _discoveredCreators.Values];
        return Task.FromResult(snapshot);
    }

    public Task<IReadOnlyList<LiveStream>> GetActiveStreamsAsync(CancellationToken ct = default)
    {
        var sessions = _streaming.GetActiveStreams();
        var streams = new List<LiveStream>(sessions.Count);

        foreach (var session in sessions)
        {
            if (session.State != StreamState.Live)
                continue;

            streams.Add(new LiveStream(
                StreamId: session.Id,
                Title: session.Title,
                CreatorUhid: session.PublisherUhid,
                Codec: session.Codec,
                SegmentDurationMs: session.SegmentDurationMs,
                StartedAtMs: new DateTimeOffset(session.StartedAt).ToUnixTimeMilliseconds(),
                ViewerCount: 0,
                IsActive: true,
                Tags: []));
        }

        return Task.FromResult<IReadOnlyList<LiveStream>>(streams);
    }

    // ── Private ────────────────────────────────────────────────────────────

    private void OnPeerNegotiated(object? sender, PeerCapabilities caps)
        => EnqueueCreatorDiscovery(caps);

    private void EnqueueCreatorDiscovery(PeerCapabilities caps)
    {
        // Respect power/network policy — don't process new peer discovery when passive
        if (_guard is { MeshScanAllowed: false })
            return;

        // Check for Streaming capability using the string tag used in the hello payload
        // or the NodeCapabilities flags if they are encoded in the capability set.
        // The Aether.Core handshake advertises capability tags as strings — "streaming" is
        // the canonical tag (see HelloPayload.cs).  We also accept the enum flag form.
        var hasStreaming =
            caps.Capabilities.Contains("streaming") ||
            caps.Capabilities.Contains("Streaming");

        if (!hasStreaming)
            return;

        // Avoid duplicate resolution work
        if (_discoveredCreators.ContainsKey(caps.PeerUhid))
            return;

        // Fire-and-forget profile resolution — we don't want to block the handshake event thread
        _ = Task.Run(async () =>
        {
            MediaProfile? profile = null;
            try
            {
                profile = await _profileService.GetProfileAsync(caps.PeerUhid).ConfigureAwait(false);
            }
            catch
            {
                // Profile service unavailable or peer profile not yet published — synthesise a stub
            }

            profile ??= SynthesiseProfile(caps.PeerUhid, caps.ImplementationVersion);

            // Store and fire — idempotent: only fire once per unique UHID
            if (_discoveredCreators.TryAdd(caps.PeerUhid, profile))
                CreatorDiscovered?.Invoke(this, profile);
        });
    }

    private static MediaProfile SynthesiseProfile(string uhid, string implementationVersion) =>
        new MediaProfile(
            Uhid: uhid,
            DisplayName: $"Creator {uhid[..Math.Min(8, uhid.Length)]}",
            AvatarHash: null,
            Bio: null,
            AetherTagValue: string.Empty,
            FollowerCount: 0,
            FollowingCount: 0,
            ContentCount: 0,
            IsVerified: false,
            JoinedAtMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
}
