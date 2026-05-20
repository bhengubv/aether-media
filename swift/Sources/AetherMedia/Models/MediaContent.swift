import Foundation

// MARK: - MediaReactionType

public enum MediaReactionType: Int, Codable, CustomStringConvertible {
    case like       = 1
    case share      = 2
    case comment    = 3
    case superReact = 4

    public var description: String {
        switch self {
        case .like:       return "Like"
        case .share:      return "Share"
        case .comment:    return "Comment"
        case .superReact: return "SuperReact"
        }
    }
}

// MARK: - MediaContent

public struct MediaContent: Codable, Identifiable, Hashable {
    public var id: String { contentHash }

    public let contentHash: String
    public let title: String
    public let durationMs: Int64
    public let codec: String
    public let contentType: String
    public let creatorUhid: String
    public let sizeBytes: Int64
    public let thumbnailHash: String?
    public let tags: [String]

    public init(
        contentHash: String,
        title: String,
        durationMs: Int64,
        codec: String,
        contentType: String,
        creatorUhid: String,
        sizeBytes: Int64,
        thumbnailHash: String? = nil,
        tags: [String] = []
    ) {
        self.contentHash   = contentHash
        self.title         = title
        self.durationMs    = durationMs
        self.codec         = codec
        self.contentType   = contentType
        self.creatorUhid   = creatorUhid
        self.sizeBytes     = sizeBytes
        self.thumbnailHash = thumbnailHash
        self.tags          = tags
    }

    /// Human-readable duration:
    /// - 0 ms → "Live"
    /// - < 1 hour → "M:SS"
    /// - >= 1 hour → "H:MM:SS"
    public var formattedDuration: String {
        guard durationMs > 0 else { return "Live" }
        let totalSeconds = Int(durationMs / 1000)
        let hours   = totalSeconds / 3600
        let minutes = (totalSeconds % 3600) / 60
        let seconds = totalSeconds % 60
        if hours > 0 {
            return String(format: "%d:%02d:%02d", hours, minutes, seconds)
        }
        return String(format: "%d:%02d", minutes, seconds)
    }

    public var isVideo: Bool { contentType.lowercased().hasPrefix("video/") }
    public var isAudio: Bool { contentType.lowercased().hasPrefix("audio/") }
}

// MARK: - MediaReaction

public enum MediaReactionError: Error, LocalizedError {
    case emptyContentHash
    case emptyFromUhid
    case commentRequiresMessage
    case nonCommentMustNotHaveMessage(MediaReactionType)

    public var errorDescription: String? {
        switch self {
        case .emptyContentHash:
            return "contentHash must not be empty"
        case .emptyFromUhid:
            return "fromUhid must not be empty"
        case .commentRequiresMessage:
            return "A message is required for Comment reactions"
        case .nonCommentMustNotHaveMessage(let type):
            return "message must be nil for \(type) reactions"
        }
    }
}

public struct MediaReaction: Codable, Identifiable {
    public var id: String { reactionId }

    public let reactionId: String
    public let contentHash: String
    public let fromUhid: String
    public let type: MediaReactionType
    public let positionMs: Int64
    public let message: String?
    public let sentAt: Date

    /// Failable init that validates business rules.
    public init(
        reactionId: String,
        contentHash: String,
        fromUhid: String,
        type: MediaReactionType,
        positionMs: Int64,
        message: String?,
        sentAt: Date
    ) throws {
        guard !contentHash.trimmingCharacters(in: .whitespaces).isEmpty else {
            throw MediaReactionError.emptyContentHash
        }
        guard !fromUhid.trimmingCharacters(in: .whitespaces).isEmpty else {
            throw MediaReactionError.emptyFromUhid
        }
        if type == .comment {
            guard let msg = message, !msg.trimmingCharacters(in: .whitespaces).isEmpty else {
                throw MediaReactionError.commentRequiresMessage
            }
        } else {
            if message != nil {
                throw MediaReactionError.nonCommentMustNotHaveMessage(type)
            }
        }
        self.reactionId  = reactionId
        self.contentHash = contentHash
        self.fromUhid    = fromUhid
        self.type        = type
        self.positionMs  = positionMs
        self.message     = message
        self.sentAt      = sentAt
    }
}

// MARK: - MediaProfile

public struct MediaProfile: Codable, Identifiable {
    public var id: String { uhid }

    public let uhid: String
    public let displayName: String
    public let avatarHash: String?
    public let bio: String?
    public let aetherTagValue: String
    public let followerCount: Int
    public let followingCount: Int
    public let contentCount: Int
    public let isVerified: Bool
    public let joinedAt: Date

    private static let shortBioMax = 120

    /// Bio trimmed to 120 chars at the last word boundary, with "…" appended.
    /// Returns "" when bio is nil or whitespace.
    public var shortBio: String {
        guard let bio, !bio.trimmingCharacters(in: .whitespaces).isEmpty else { return "" }
        let trimmed = bio.trimmingCharacters(in: .whitespaces)
        guard trimmed.count > MediaProfile.shortBioMax else { return trimmed }

        let cutIndex = trimmed.index(trimmed.startIndex, offsetBy: MediaProfile.shortBioMax)
        let cut = String(trimmed[..<cutIndex])
        if let lastSpaceRange = cut.range(of: " ", options: .backwards) {
            let boundary = String(cut[..<lastSpaceRange.lowerBound]).trimmingCharacters(in: .whitespaces)
            return boundary + "…"
        }
        return cut.trimmingCharacters(in: .whitespaces) + "…"
    }
}

// MARK: - LiveStream

public struct LiveStream: Codable, Identifiable {
    public var id: String { streamId }

    public let streamId: String
    public let title: String
    public let creatorUhid: String
    public let codec: String
    public let segmentDurationMs: Int
    public let startedAt: Date
    public let viewerCount: Int
    public let isActive: Bool
    public let tags: [String]

    /// Wall-clock milliseconds since the broadcast started.  Always >= 0.
    public var elapsedMs: Int64 {
        let elapsed = Int64(Date().timeIntervalSince(startedAt) * 1000)
        return max(0, elapsed)
    }

    /// Human-readable elapsed time (H:MM:SS or M:SS).
    public var elapsedFormatted: String {
        let totalSeconds = Int(elapsedMs / 1000)
        let hours   = totalSeconds / 3600
        let minutes = (totalSeconds % 3600) / 60
        let seconds = totalSeconds % 60
        if hours > 0 {
            return String(format: "%d:%02d:%02d", hours, minutes, seconds)
        }
        return String(format: "%d:%02d", minutes, seconds)
    }
}

// MARK: - MediaFeedItem

public struct MediaFeedItem: Codable, Identifiable {
    public var id: String { content.contentHash }

    public let content: MediaContent
    public let likeCount: Int
    public let shareCount: Int
    public let commentCount: Int
    public let watchCount: Int
    public let isLive: Bool
    public let streamId: String?
    public let topReactions: [MediaReaction]
    public let publishedAt: Date
    public let watchedMs: Int64

    public init(
        content: MediaContent,
        likeCount: Int,
        shareCount: Int,
        commentCount: Int,
        watchCount: Int,
        isLive: Bool,
        streamId: String?,
        topReactions: [MediaReaction],
        publishedAt: Date,
        watchedMs: Int64 = 0
    ) {
        self.content      = content
        self.likeCount    = likeCount
        self.shareCount   = shareCount
        self.commentCount = commentCount
        self.watchCount   = watchCount
        self.isLive       = isLive
        self.streamId     = streamId
        self.topReactions = topReactions
        self.publishedAt  = publishedAt
        self.watchedMs    = watchedMs
    }

    /// True when published within the last 24 hours.
    public var isNew: Bool { Date().timeIntervalSince(publishedAt) < 86_400 }

    /// Likes + shares + comments.
    public var reactionTotal: Int { likeCount + shareCount + commentCount }
}
