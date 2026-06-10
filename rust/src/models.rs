use serde::{Deserialize, Serialize};
use std::fmt;

// ── Reaction type ─────────────────────────────────────────────────────────────

#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "snake_case")]
#[repr(u8)]
pub enum MediaReactionType {
    Like       = 1,
    Share      = 2,
    Comment    = 3,
    SuperReact = 4,
}

impl fmt::Display for MediaReactionType {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            MediaReactionType::Like       => write!(f, "Like"),
            MediaReactionType::Share      => write!(f, "Share"),
            MediaReactionType::Comment    => write!(f, "Comment"),
            MediaReactionType::SuperReact => write!(f, "SuperReact"),
        }
    }
}

// ── MediaContent ──────────────────────────────────────────────────────────────

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct MediaContent {
    pub content_hash: String,
    pub title: String,
    /// Duration in milliseconds. 0 means live.
    pub duration_ms: u64,
    pub codec: String,
    pub content_type: String,
    pub creator_uhid: String,
    pub size_bytes: u64,
    /// Unix-epoch milliseconds when the content was created/published.
    pub created_at_ms: u64,
    pub thumbnail_hash: Option<String>,
    pub tags: Vec<String>,
}

impl MediaContent {
    /// Returns "Live" when duration_ms is 0.
    /// Returns "M:SS" when less than one hour.
    /// Returns "H:MM:SS" when >= 1 hour.
    pub fn formatted_duration(&self) -> String {
        if self.duration_ms == 0 {
            return "Live".to_owned();
        }
        let total_secs = self.duration_ms / 1000;
        let hours   = total_secs / 3600;
        let minutes = (total_secs % 3600) / 60;
        let seconds = total_secs % 60;
        if hours > 0 {
            format!("{hours}:{minutes:02}:{seconds:02}")
        } else {
            format!("{minutes}:{seconds:02}")
        }
    }

    pub fn is_video(&self) -> bool {
        self.content_type.to_ascii_lowercase().starts_with("video/")
    }

    pub fn is_audio(&self) -> bool {
        self.content_type.to_ascii_lowercase().starts_with("audio/")
    }
}

// ── MediaReaction ─────────────────────────────────────────────────────────────

/// A validated reaction.  Construct with [`MediaReaction::new`].
#[derive(Debug, Clone, PartialEq, Eq, Serialize, Deserialize)]
pub struct MediaReaction {
    pub reaction_id: String,
    pub content_hash: String,
    pub from_uhid: String,
    #[serde(rename = "type")]
    pub reaction_type: MediaReactionType,
    pub position_ms: u64,
    pub message: Option<String>,
    pub sent_at_ms: u64, // epoch ms
}

#[derive(Debug, PartialEq, Eq)]
pub enum ReactionError {
    EmptyContentHash,
    EmptyFromUhid,
    CommentRequiresMessage,
    NonCommentMustNotHaveMessage,
}

impl fmt::Display for ReactionError {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            ReactionError::EmptyContentHash             => write!(f, "content_hash must not be empty"),
            ReactionError::EmptyFromUhid                => write!(f, "from_uhid must not be empty"),
            ReactionError::CommentRequiresMessage       => write!(f, "a message is required for Comment reactions"),
            ReactionError::NonCommentMustNotHaveMessage => write!(f, "message must be None for non-Comment reactions"),
        }
    }
}

impl MediaReaction {
    pub fn new(
        reaction_id: String,
        content_hash: String,
        from_uhid: String,
        reaction_type: MediaReactionType,
        position_ms: u64,
        message: Option<String>,
        sent_at_ms: u64,
    ) -> Result<Self, ReactionError> {
        if content_hash.trim().is_empty() {
            return Err(ReactionError::EmptyContentHash);
        }
        if from_uhid.trim().is_empty() {
            return Err(ReactionError::EmptyFromUhid);
        }
        if reaction_type == MediaReactionType::Comment {
            match &message {
                None => return Err(ReactionError::CommentRequiresMessage),
                Some(m) if m.trim().is_empty() => return Err(ReactionError::CommentRequiresMessage),
                _ => {}
            }
        } else if message.is_some() {
            return Err(ReactionError::NonCommentMustNotHaveMessage);
        }
        Ok(Self { reaction_id, content_hash, from_uhid, reaction_type, position_ms, message, sent_at_ms })
    }
}

// ── MediaProfile ──────────────────────────────────────────────────────────────

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct MediaProfile {
    pub uhid: String,
    pub display_name: String,
    pub avatar_hash: Option<String>,
    pub bio: Option<String>,
    pub aethernet_tag: String,
    pub follower_count: u32,
    pub following_count: u32,
    pub content_count: u32,
    pub is_verified: bool,
    pub joined_at_ms: u64,
}

impl MediaProfile {
    const SHORT_BIO_MAX: usize = 120;

    /// Bio trimmed to 120 chars at the last word boundary, with "…" appended.
    /// Returns empty string when bio is None or whitespace.
    pub fn short_bio(&self) -> String {
        let bio = match &self.bio {
            None => return String::new(),
            Some(b) => b.trim().to_owned(),
        };
        if bio.is_empty() {
            return String::new();
        }
        if bio.chars().count() <= Self::SHORT_BIO_MAX {
            return bio;
        }
        // Collect chars up to limit, then find last space boundary
        let cut: String = bio.chars().take(Self::SHORT_BIO_MAX).collect();
        let boundary = cut.rfind(' ').unwrap_or(cut.len());
        let trimmed = cut[..boundary].trim_end();
        format!("{trimmed}…")
    }
}

// ── LiveStream ────────────────────────────────────────────────────────────────

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct LiveStream {
    pub stream_id: String,
    pub title: String,
    pub creator_uhid: String,
    pub codec: String,
    pub segment_duration_ms: u32,
    pub started_at_ms: u64,
    pub viewer_count: u32,
    pub is_active: bool,
    pub tags: Vec<String>,
}

impl LiveStream {
    /// Elapsed wall-clock milliseconds since the stream started.
    /// Returns 0 if the system clock is somehow behind started_at_ms.
    pub fn elapsed_ms(&self) -> u64 {
        use std::time::{SystemTime, UNIX_EPOCH};
        let now_ms = SystemTime::now()
            .duration_since(UNIX_EPOCH)
            .map(|d| d.as_millis() as u64)
            .unwrap_or(self.started_at_ms);
        now_ms.saturating_sub(self.started_at_ms)
    }

    /// Human-readable elapsed time (H:MM:SS or M:SS).
    pub fn elapsed_formatted(&self) -> String {
        let total_secs = self.elapsed_ms() / 1000;
        let hours   = total_secs / 3600;
        let minutes = (total_secs % 3600) / 60;
        let seconds = total_secs % 60;
        if hours > 0 {
            format!("{hours}:{minutes:02}:{seconds:02}")
        } else {
            format!("{minutes}:{seconds:02}")
        }
    }
}

// ── Tests ─────────────────────────────────────────────────────────────────────

#[cfg(test)]
mod tests {
    use super::*;

    fn sample_content(duration_ms: u64) -> MediaContent {
        MediaContent {
            content_hash: "abc".into(),
            title: "Test".into(),
            duration_ms,
            codec: "h264".into(),
            content_type: "video/mp4".into(),
            creator_uhid: "u1".into(),
            size_bytes: 1000,
            created_at_ms: 1_700_000_000_000,
            thumbnail_hash: None,
            tags: vec![],
        }
    }

    #[test]
    fn formatted_duration_live() {
        assert_eq!(sample_content(0).formatted_duration(), "Live");
    }

    #[test]
    fn formatted_duration_sub_hour() {
        // 4:32 = 272 seconds
        assert_eq!(sample_content(272_000).formatted_duration(), "4:32");
    }

    #[test]
    fn formatted_duration_over_hour() {
        // 1:23:45 = 5025 seconds
        assert_eq!(sample_content(5_025_000).formatted_duration(), "1:23:45");
    }

    #[test]
    fn is_video_and_audio() {
        let v = sample_content(1000);
        assert!(v.is_video());
        assert!(!v.is_audio());
    }

    #[test]
    fn reaction_comment_requires_message() {
        let err = MediaReaction::new(
            "r1".into(), "hash".into(), "u1".into(),
            MediaReactionType::Comment, 0, None, 0,
        );
        assert_eq!(err, Err(ReactionError::CommentRequiresMessage));
    }

    #[test]
    fn reaction_like_must_not_have_message() {
        let err = MediaReaction::new(
            "r1".into(), "hash".into(), "u1".into(),
            MediaReactionType::Like, 0, Some("oops".into()), 0,
        );
        assert_eq!(err, Err(ReactionError::NonCommentMustNotHaveMessage));
    }

    #[test]
    fn reaction_comment_valid() {
        let r = MediaReaction::new(
            "r1".into(), "hash".into(), "u1".into(),
            MediaReactionType::Comment, 500, Some("Nice!".into()), 0,
        ).unwrap();
        assert_eq!(r.message.as_deref(), Some("Nice!"));
    }
}
