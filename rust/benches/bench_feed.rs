use criterion::{black_box, criterion_group, criterion_main, Criterion};

use aethermedia::feed::{FeedAggregator, MediaFeedItem, FEED_CAP};
use aethermedia::models::MediaContent;

fn make_item(i: usize) -> MediaFeedItem {
    MediaFeedItem {
        content: MediaContent {
            content_hash: format!("hash-{i:05}"),
            title: format!("Video {i}"),
            duration_ms: 60_000,
            codec: "h264".into(),
            content_type: "video/mp4".into(),
            creator_uhid: "creator-1".into(),
            size_bytes: 10_000_000,
            thumbnail_hash: None,
            tags: vec![],
        },
        like_count: i as u32,
        share_count: 0,
        comment_count: 0,
        watch_count: 0,
        is_live: false,
        stream_id: None,
        published_at_ms: i as u64 * 1000,
        watched_ms: 0,
    }
}

fn bench_get_feed(c: &mut Criterion) {
    // Pre-fill the aggregator to capacity
    let mut feed = FeedAggregator::new();
    for i in 0..FEED_CAP {
        feed.add_item(make_item(i));
    }

    c.bench_function("get_feed_limit_20_offset_0", |b| {
        b.iter(|| {
            let page = feed.get_feed(black_box(20), black_box(0));
            black_box(page.len());
        });
    });

    c.bench_function("get_feed_full_page", |b| {
        b.iter(|| {
            let page = feed.get_feed(black_box(FEED_CAP), black_box(0));
            black_box(page.len());
        });
    });

    c.bench_function("get_feed_offset_midpoint", |b| {
        b.iter(|| {
            let page = feed.get_feed(black_box(20), black_box(FEED_CAP / 2));
            black_box(page.len());
        });
    });
}

fn bench_add_item(c: &mut Criterion) {
    c.bench_function("add_500_items", |b| {
        b.iter(|| {
            let mut feed = FeedAggregator::new();
            for i in 0..FEED_CAP {
                feed.add_item(make_item(black_box(i)));
            }
            black_box(feed.len());
        });
    });
}

criterion_group!(benches, bench_get_feed, bench_add_item);
criterion_main!(benches);
