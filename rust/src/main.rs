use aethermedia::feed::{FeedAggregator, MediaFeedItem};
use aethermedia::models::{MediaContent, MediaReaction, MediaReactionType, MediaProfile};
use aethermedia::social::SocialGraph;

fn main() {
    // ── Social graph demo ────────────────────────────────────────────────────
    let mut graph = SocialGraph::new();
    graph.follow("alice-uhid-0001");
    graph.follow("bob-uhid-0002");
    graph.follow("carol-uhid-0003");

    println!("=== Social Graph ===");
    println!("Following {} account(s):", graph.following_count());
    for uhid in graph.following_list() {
        println!("  • {uhid}");
    }

    graph.unfollow("bob-uhid-0002");
    println!("After unfollowing bob: {} account(s)", graph.following_count());

    // ── MediaContent demo ────────────────────────────────────────────────────
    let content = MediaContent {
        content_hash: "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855".into(),
        title: "Aether Launch Stream".into(),
        duration_ms: 5_025_000,
        codec: "h264".into(),
        content_type: "video/mp4".into(),
        creator_uhid: "alice-uhid-0001".into(),
        size_bytes: 150_000_000,
        thumbnail_hash: Some("thumb_abc".into()),
        tags: vec!["aether".into(), "launch".into(), "live".into()],
    };

    println!("\n=== MediaContent ===");
    println!("Title:    {}", content.title);
    println!("Duration: {}", content.formatted_duration());
    println!("IsVideo:  {}", content.is_video());
    println!("Tags:     {:?}", content.tags);

    // ── MediaProfile demo ────────────────────────────────────────────────────
    let profile = MediaProfile {
        uhid: "alice-uhid-0001".into(),
        display_name: "Alice".into(),
        avatar_hash: None,
        bio: Some("Building on the Aether mesh. Decentralised video for everyone. South African creator. Building open-source tools for the mesh network since day one.".into()),
        aethernet_tag: "@alice".into(),
        follower_count: 1_234,
        following_count: 56,
        content_count: 42,
        is_verified: true,
        joined_at_ms: 1_700_000_000_000,
    };
    println!("\n=== MediaProfile ===");
    println!("DisplayName: {}", profile.display_name);
    println!("ShortBio:    {}", profile.short_bio());

    // ── Feed aggregator demo ─────────────────────────────────────────────────
    let mut feed = FeedAggregator::new();

    for i in 0..5u32 {
        feed.add_item(MediaFeedItem {
            content: MediaContent {
                content_hash: format!("hash-{i:03}"),
                title: format!("Video #{i}"),
                duration_ms: (i as u64 + 1) * 60_000,
                codec: "h264".into(),
                content_type: "video/mp4".into(),
                creator_uhid: "alice-uhid-0001".into(),
                size_bytes: 10_000_000,
                thumbnail_hash: None,
                tags: vec![],
            },
            like_count: i * 10,
            share_count: i * 2,
            comment_count: i,
            watch_count: 0,
            is_live: false,
            stream_id: None,
            published_at_ms: 1_700_000_000_000 + (i as u64 * 3600_000),
            watched_ms: 0,
        });
    }

    feed.mark_watched("hash-004", 45_000);

    println!("\n=== Feed (first 3) ===");
    for item in feed.get_feed(3, 0) {
        let watched = if item.watched_ms > 0 {
            format!(" [watched {}ms]", item.watched_ms)
        } else {
            String::new()
        };
        println!(
            "  {} | {} likes | {}{}",
            item.content.title, item.like_count,
            item.content.formatted_duration(), watched,
        );
    }

    // ── Reaction validation demo ─────────────────────────────────────────────
    println!("\n=== Reactions ===");
    let ok = MediaReaction::new(
        "r-001".into(),
        "hash-004".into(),
        "bob-uhid-0002".into(),
        MediaReactionType::Comment,
        12_500,
        Some("Great stream!".into()),
        1_700_000_000_000,
    );
    match ok {
        Ok(r)  => println!("Comment reaction: {:?}", r.message),
        Err(e) => println!("Reaction error: {e}"),
    }

    let bad = MediaReaction::new(
        "r-002".into(),
        "hash-004".into(),
        "carol-uhid-0003".into(),
        MediaReactionType::Comment,
        0,
        None,
        1_700_000_000_000,
    );
    match bad {
        Ok(_)  => println!("Unexpected success"),
        Err(e) => println!("Expected validation error: {e}"),
    }
}
