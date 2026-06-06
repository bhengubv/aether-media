// SPDX-License-Identifier: MIT
using Microsoft.Extensions.Logging;

namespace AetherMesh.Media.Streaming;

/// <summary>
/// Integrates ISosBroadcastService with active watch sessions and live streams.
/// On SOS activation, all media playback is immediately suspended and a
/// full-screen emergency notice is broadcast to all active participants.
/// </summary>
public interface ISosAwareMediaSession
{
    /// <summary>Suspend this session immediately for an SOS broadcast.</summary>
    Task SuspendForSosAsync(string emergencyMessage, CancellationToken ct = default);

    /// <summary>Resume normal playback after SOS is cleared.</summary>
    Task ResumeAfterSosAsync(CancellationToken ct = default);
}

public sealed class SosBroadcastIntegration : ISosAwareMediaSession
{
    private readonly ILogger<SosBroadcastIntegration>? _log;
    private volatile bool _suspended;

    public SosBroadcastIntegration(ILogger<SosBroadcastIntegration>? log = null)
        => _log = log;

    public Task SuspendForSosAsync(string emergencyMessage, CancellationToken ct = default)
    {
        _suspended = true;
        _log?.LogWarning("SOS activated — suspending media session: {Message}", emergencyMessage);
        OnSosSuspended?.Invoke(this, emergencyMessage);
        return Task.CompletedTask;
    }

    public Task ResumeAfterSosAsync(CancellationToken ct = default)
    {
        _suspended = false;
        _log?.LogInformation("SOS cleared — resuming media session");
        OnSosCleared?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public bool IsSuspended => _suspended;

    public event EventHandler<string>?   OnSosSuspended;
    public event EventHandler<EventArgs>? OnSosCleared;
}
