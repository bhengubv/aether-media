// SPDX-License-Identifier: MIT
// Aether Media — In-Process Relay Round-Trip Test
//
// Validates that two Aether Media nodes can exchange a MediaContent descriptor
// through the Aether in-process transport — the same ITransportService interface
// used by BLE, Wi-Fi Direct, NearLink, and HTTP relay transports.
//
// How to run:
//   dotnet run --project samples/AetherMedia.RelayTest
//
// What it does:
//   1. Creates two InProcessTransportService nodes (node-a, node-b).
//   2. Node A serialises a MediaContent descriptor as JSON bytes.
//   3. Node A sends those bytes to node B via ITransportService.SendAsync.
//   4. Node B receives via the DataReceived action.
//   5. Verifies the received payload matches the sent descriptor.
//   6. Prints round-trip latency and exits 0 on success, 1 on failure.

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AetherNet.Transport.Services;
using Microsoft.Extensions.Logging;

const string NodeAId = "relay-node-a";
const string NodeBId = "relay-node-b";

Console.WriteLine("Aether Media — In-Process Relay Round-Trip Test");
Console.WriteLine("─────────────────────────────────────────────────");
Console.WriteLine($"Node A: {NodeAId}");
Console.WriteLine($"Node B: {NodeBId}");
Console.WriteLine();

// ── 1. Create transport nodes ─────────────────────────────────────────────────

using var logFactory = LoggerFactory.Create(builder =>
    builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

using var nodeA = new InProcessTransportService(
    NodeAId,
    logFactory.CreateLogger<InProcessTransportService>());

using var nodeB = new InProcessTransportService(
    NodeBId,
    logFactory.CreateLogger<InProcessTransportService>());

// ── 2. Build a MediaContent descriptor and serialise it ───────────────────────

// MediaContent is from AetherMedia.Core.  We serialise it to UTF-8 JSON and
// transmit the raw bytes — exactly how the social layer would gossip a content
// announcement over the mesh.

var content = new MediaContentDescriptor
{
    ContentHash  = "sha256-relay-test-001",
    Title        = "Relay Test Video",
    DurationMs   = 60_000,
    Codec        = "H.264",
    ContentType  = "video/mp4",
    CreatorUhid  = "KXJB7-MN2P4",
    CreatedAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
};

var jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(content));
Console.WriteLine($"[node-a] Payload: {jsonBytes.Length}B — \"{content.Title}\"  hash={content.ContentHash}");

// ── 3. Wire node B receive handler ────────────────────────────────────────────

var receivedTcs = new TaskCompletionSource<(string From, byte[] Data)>(
    TaskCreationOptions.RunContinuationsAsynchronously);

nodeB.DataReceived += (from, data) =>
{
    Console.WriteLine($"[node-b] RX {data.Length}B from {from}");
    receivedTcs.TrySetResult((from, data));
};

// ── 4. Send and measure latency ───────────────────────────────────────────────

Console.WriteLine($"[node-a] Sending {jsonBytes.Length}B → {NodeBId}");

var stopwatch = Stopwatch.StartNew();
var sent = await nodeA.SendAsync(NodeBId, jsonBytes);
stopwatch.Stop();

if (!sent)
{
    Console.Error.WriteLine("FAIL: nodeA.SendAsync returned false.");
    return 1;
}

Console.WriteLine($"[node-a] Send completed in {stopwatch.ElapsedMilliseconds} ms");

// ── 5. Wait for node B to receive the payload ─────────────────────────────────

using var receiveTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

(string From, byte[] Data) received;
try
{
    received = await receivedTcs.Task.WaitAsync(receiveTimeout.Token);
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("FAIL: Node B did not receive the payload within 5 seconds.");
    return 1;
}

var endToEndMs = stopwatch.ElapsedMilliseconds;

// ── 6. Verify the round-trip ──────────────────────────────────────────────────

var receivedDescriptor = JsonSerializer.Deserialize<MediaContentDescriptor>(
    Encoding.UTF8.GetString(received.Data));

if (receivedDescriptor?.ContentHash != content.ContentHash)
{
    Console.Error.WriteLine(
        $"FAIL: ContentHash mismatch. Expected {content.ContentHash}, " +
        $"got {receivedDescriptor?.ContentHash}");
    return 1;
}

if (receivedDescriptor.Title != content.Title)
{
    Console.Error.WriteLine(
        $"FAIL: Title mismatch. Expected \"{content.Title}\", " +
        $"got \"{receivedDescriptor.Title}\"");
    return 1;
}

// ── 7. Print summary ──────────────────────────────────────────────────────────

Console.WriteLine();
Console.WriteLine("─────────────────────────────────────────────────");
Console.WriteLine("PASS — Relay round-trip verified.");
Console.WriteLine($"  From:         {received.From}");
Console.WriteLine($"  ContentHash:  {receivedDescriptor!.ContentHash}");
Console.WriteLine($"  Title:        {receivedDescriptor.Title}");
Console.WriteLine($"  Codec:        {receivedDescriptor.Codec}");
Console.WriteLine($"  Latency:      {endToEndMs} ms  (send → DataReceived)");
Console.WriteLine("─────────────────────────────────────────────────");

return 0;

// ── Wire DTO ──────────────────────────────────────────────────────────────────

/// <summary>
/// Minimal wire representation of a MediaContent descriptor.
/// Mirrors <c>AetherMedia.Core.Models.MediaContent</c> for serialisation
/// without taking a project dependency on AetherMedia.Core from this sample.
/// </summary>
internal sealed class MediaContentDescriptor
{
    public string ContentHash  { get; init; } = string.Empty;
    public string Title        { get; init; } = string.Empty;
    public long   DurationMs   { get; init; }
    public string Codec        { get; init; } = string.Empty;
    public string ContentType  { get; init; } = string.Empty;
    public string CreatorUhid  { get; init; } = string.Empty;
    public long   CreatedAtMs  { get; init; }
}
