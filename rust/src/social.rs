use std::collections::HashSet;

/// In-memory social graph for one local user.
///
/// Stores the set of UHIDs this user is following.  Thread-safe access is
/// not provided at this layer — wrap in Arc<Mutex<SocialGraph>> when
/// sharing across threads.
pub struct SocialGraph {
    following: HashSet<String>,
}

impl SocialGraph {
    pub fn new() -> Self {
        Self { following: HashSet::new() }
    }

    /// Add `uhid` to the following set.  No-op if already following.
    pub fn follow(&mut self, uhid: &str) {
        self.following.insert(uhid.to_owned());
    }

    /// Remove `uhid` from the following set.  No-op if not following.
    pub fn unfollow(&mut self, uhid: &str) {
        self.following.remove(uhid);
    }

    /// Returns true if `uhid` is in the following set.
    pub fn is_following(&self, uhid: &str) -> bool {
        self.following.contains(uhid)
    }

    /// Returns a sorted Vec of all followed UHIDs.
    /// Sorted so output is deterministic in tests.
    pub fn following_list(&self) -> Vec<&str> {
        let mut list: Vec<&str> = self.following.iter().map(String::as_str).collect();
        list.sort_unstable();
        list
    }

    pub fn following_count(&self) -> usize {
        self.following.len()
    }
}

impl Default for SocialGraph {
    fn default() -> Self {
        Self::new()
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn follow_and_unfollow() {
        let mut g = SocialGraph::new();
        g.follow("alice");
        g.follow("bob");
        assert!(g.is_following("alice"));
        assert!(g.is_following("bob"));
        assert_eq!(g.following_count(), 2);

        g.unfollow("alice");
        assert!(!g.is_following("alice"));
        assert_eq!(g.following_count(), 1);
    }

    #[test]
    fn double_follow_is_idempotent() {
        let mut g = SocialGraph::new();
        g.follow("alice");
        g.follow("alice");
        assert_eq!(g.following_count(), 1);
    }

    #[test]
    fn unfollow_not_following_is_noop() {
        let mut g = SocialGraph::new();
        g.unfollow("ghost"); // should not panic
        assert_eq!(g.following_count(), 0);
    }

    #[test]
    fn following_list_sorted() {
        let mut g = SocialGraph::new();
        g.follow("charlie");
        g.follow("alice");
        g.follow("bob");
        let list = g.following_list();
        assert_eq!(list, vec!["alice", "bob", "charlie"]);
    }
}
