import Foundation

/// Thread-safe social graph backed by a Swift `actor`.
///
/// Tracks which UHIDs the local user is following.  Because it's an actor
/// all mutations and reads are serialised on the actor's executor, making
/// this safe to use from multiple async contexts simultaneously.
public actor SocialGraph {
    private var following: Set<String> = []

    public init() {}

    /// Add `uhid` to the following set.  No-op if already following.
    public func follow(_ uhid: String) {
        guard !uhid.trimmingCharacters(in: .whitespaces).isEmpty else { return }
        following.insert(uhid)
    }

    /// Remove `uhid` from the following set.  No-op if not following.
    public func unfollow(_ uhid: String) {
        following.remove(uhid)
    }

    /// Returns true when `uhid` is in the following set.
    public func isFollowing(_ uhid: String) -> Bool {
        following.contains(uhid)
    }

    /// Returns a sorted array of all followed UHIDs.
    public func followingList() -> [String] {
        following.sorted()
    }

    /// Number of followed accounts.
    public var count: Int { following.count }
}
