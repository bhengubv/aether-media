// SPDX-License-Identifier: MIT

namespace AetherMedia.Ingest;

/// <summary>
/// A running ingest: the live mesh stream id plus lifecycle control. Returned by
/// <see cref="IStreamGateway.StartAsync"/>; the pump runs in the background and <see cref="Completion"/>
/// settles when the source ends or the session is stopped.
/// </summary>
public sealed class IngestSession : IAsyncDisposable
{
    private readonly CancellationTokenSource _cts;
    private int _stopped;

    internal IngestSession(Guid streamId, Task completion, CancellationTokenSource cts)
    {
        StreamId = streamId;
        Completion = completion;
        _cts = cts;
    }

    /// <summary>The mesh stream id, live from the moment <see cref="IStreamGateway.StartAsync"/> returns.</summary>
    public Guid StreamId { get; }

    /// <summary>Completes when the source ends or the session is stopped.</summary>
    public Task Completion { get; }

    /// <summary>Stop ingesting and end the mesh stream. Idempotent.</summary>
    public async Task StopAsync()
    {
        if (Interlocked.Exchange(ref _stopped, 1) != 0)
        {
            return;
        }

        await _cts.CancelAsync().ConfigureAwait(false);
        try
        {
            await Completion.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected: cancelling the pump surfaces as a cancellation.
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _cts.Dispose();
    }
}
