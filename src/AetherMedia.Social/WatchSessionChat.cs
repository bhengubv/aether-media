// SPDX-License-Identifier: MIT
namespace AetherMedia.Social;

/// <summary>
/// In-session chat attached to a watch party or live stream.
/// Messages are delivered via IMessagingService on the Aether mesh.
/// </summary>
public sealed class ChatMessage
{
    public string   MessageId  { get; init; } = Guid.NewGuid().ToString();
    public string   SessionId  { get; init; } = "";
    public string   FromUhid   { get; init; } = "";
    public string   Text       { get; init; } = "";
    public long     SentAtMs   { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public long?    PositionMs { get; init; }   // null = not timestamped
}

/// <summary>
/// Holds the chat history for a single watch session or live stream.
/// The actual transport is IMessagingService; this is the local store.
/// </summary>
public sealed class WatchSessionChat
{
    private readonly List<ChatMessage> _messages = [];
    private readonly Lock _lock = new();

    public string SessionId { get; }

    public WatchSessionChat(string sessionId) => SessionId = sessionId;

    public void AddMessage(ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        lock (_lock) { _messages.Add(message); }
        MessageReceived?.Invoke(this, message);
    }

    public IReadOnlyList<ChatMessage> GetMessages(int limit = 100)
    {
        lock (_lock)
            return _messages.TakeLast(limit).ToList();
    }

    public IReadOnlyList<ChatMessage> GetMessagesNear(long positionMs, long windowMs = 5_000)
    {
        lock (_lock)
            return _messages
                .Where(m => m.PositionMs.HasValue
                         && Math.Abs(m.PositionMs.Value - positionMs) <= windowMs)
                .ToList();
    }

    public event EventHandler<ChatMessage>? MessageReceived;
}
