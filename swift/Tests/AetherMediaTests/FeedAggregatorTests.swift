import XCTest
@testable import AetherMedia

final class FeedAggregatorTests: XCTestCase {

    // MARK: - Helpers

    private func makeContent(hash: String = "h1") -> MediaContent {
        MediaContent(
            contentHash:  hash,
            title:        "Title \(hash)",
            durationMs:   60_000,
            codec:        "h264",
            contentType:  "video/mp4",
            creatorUhid:  "creator",
            sizeBytes:    500_000,
            createdAtMs:  0
        )
    }

    private func makeFeedItem(hash: String = "h1", publishedAtMs: Int64 = 0) -> MediaFeedItem {
        MediaFeedItem(
            content:       makeContent(hash: hash),
            likeCount:     0,
            shareCount:    0,
            commentCount:  0,
            watchCount:    0,
            isLive:        false,
            streamId:      nil,
            topReactions:  [],
            publishedAtMs: publishedAtMs
        )
    }

    // MARK: - count

    func testCountStartsAtZero() async {
        let agg = FeedAggregator()
        let c = await agg.count
        XCTAssertEqual(c, 0)
    }

    // MARK: - addItem / getFeed

    func testAddSingleItem() async {
        let agg = FeedAggregator()
        await agg.addItem(makeFeedItem(hash: "h1"))
        let feed = await agg.getFeed(limit: 10, offset: 0)
        XCTAssertEqual(feed.count, 1)
        XCTAssertEqual(feed[0].content.contentHash, "h1")
    }

    func testItemsStoredNewestFirst() async {
        let agg = FeedAggregator()
        await agg.addItem(makeFeedItem(hash: "first"))
        await agg.addItem(makeFeedItem(hash: "second"))
        let feed = await agg.getFeed(limit: 10, offset: 0)
        XCTAssertEqual(feed[0].content.contentHash, "second")
        XCTAssertEqual(feed[1].content.contentHash, "first")
    }

    // MARK: - getFeed pagination

    func testGetFeedLimitZeroReturnsEmpty() async {
        let agg = FeedAggregator()
        await agg.addItem(makeFeedItem(hash: "h1"))
        let feed = await agg.getFeed(limit: 0, offset: 0)
        XCTAssertTrue(feed.isEmpty)
    }

    func testGetFeedNegativeLimitReturnsEmpty() async {
        let agg = FeedAggregator()
        await agg.addItem(makeFeedItem(hash: "h1"))
        let feed = await agg.getFeed(limit: -1, offset: 0)
        XCTAssertTrue(feed.isEmpty)
    }

    func testGetFeedOffsetBeyondCountReturnsEmpty() async {
        let agg = FeedAggregator()
        await agg.addItem(makeFeedItem(hash: "h1"))
        let feed = await agg.getFeed(limit: 10, offset: 5)
        XCTAssertTrue(feed.isEmpty)
    }

    func testGetFeedOffsetExactlyAtBoundaryReturnsEmpty() async {
        let agg = FeedAggregator()
        await agg.addItem(makeFeedItem(hash: "h1"))
        // offset == count (1) should be empty
        let feed = await agg.getFeed(limit: 10, offset: 1)
        XCTAssertTrue(feed.isEmpty)
    }

    func testGetFeedPaginationPage1() async {
        let agg = FeedAggregator()
        for i in 0..<5 {
            await agg.addItem(makeFeedItem(hash: "h\(i)"))
        }
        let page = await agg.getFeed(limit: 2, offset: 0)
        XCTAssertEqual(page.count, 2)
    }

    func testGetFeedPaginationPage2() async {
        let agg = FeedAggregator()
        for i in 0..<5 {
            await agg.addItem(makeFeedItem(hash: "h\(i)"))
        }
        let page = await agg.getFeed(limit: 2, offset: 2)
        XCTAssertEqual(page.count, 2)
    }

    func testGetFeedLastPagePartial() async {
        let agg = FeedAggregator()
        for i in 0..<5 {
            await agg.addItem(makeFeedItem(hash: "h\(i)"))
        }
        // offset 4, limit 10 → only 1 item remains
        let page = await agg.getFeed(limit: 10, offset: 4)
        XCTAssertEqual(page.count, 1)
    }

    // MARK: - capacity eviction

    func testCapacityEvictsOldestItem() async {
        let agg = FeedAggregator()
        // Fill to cap (500)
        for i in 0..<500 {
            await agg.addItem(makeFeedItem(hash: "h\(i)"))
        }
        XCTAssertEqual(await agg.count, 500)
        // Add one more — the very first item added ("h0") should be evicted
        await agg.addItem(makeFeedItem(hash: "new"))
        XCTAssertEqual(await agg.count, 500)
        // Newest item is at index 0
        let first = await agg.getFeed(limit: 1, offset: 0)
        XCTAssertEqual(first[0].content.contentHash, "new")
        // "h0" was the oldest and should be gone
        let all = await agg.getFeed(limit: 500, offset: 0)
        XCTAssertFalse(all.contains(where: { $0.content.contentHash == "h0" }))
    }

    func testAtCapacityCountStaysAtCap() async {
        let agg = FeedAggregator()
        for i in 0..<505 {
            await agg.addItem(makeFeedItem(hash: "h\(i)"))
        }
        XCTAssertEqual(await agg.count, 500)
    }

    // MARK: - markWatched

    func testMarkWatchedAccumulatesMs() async {
        let agg = FeedAggregator()
        await agg.addItem(makeFeedItem(hash: "vid1"))
        await agg.markWatched(contentHash: "vid1", ms: 1000)
        await agg.markWatched(contentHash: "vid1", ms: 2000)
        let feed = await agg.getFeed(limit: 1, offset: 0)
        XCTAssertEqual(feed[0].watchedMs, 3000)
    }

    func testMarkWatchedIncrementsWatchCount() async {
        let agg = FeedAggregator()
        await agg.addItem(makeFeedItem(hash: "vid1"))
        await agg.markWatched(contentHash: "vid1", ms: 500)
        await agg.markWatched(contentHash: "vid1", ms: 500)
        let feed = await agg.getFeed(limit: 1, offset: 0)
        XCTAssertEqual(feed[0].watchCount, 2)
    }

    func testMarkWatchedIgnoresNegativeMs() async {
        let agg = FeedAggregator()
        await agg.addItem(makeFeedItem(hash: "vid1"))
        await agg.markWatched(contentHash: "vid1", ms: -500)
        let feed = await agg.getFeed(limit: 1, offset: 0)
        XCTAssertEqual(feed[0].watchedMs, 0)
        XCTAssertEqual(feed[0].watchCount, 0)
    }

    func testMarkWatchedUnknownHashIsNoop() async {
        let agg = FeedAggregator()
        await agg.addItem(makeFeedItem(hash: "vid1"))
        // Should not throw or mutate anything
        await agg.markWatched(contentHash: "unknown", ms: 1000)
        let feed = await agg.getFeed(limit: 1, offset: 0)
        XCTAssertEqual(feed[0].watchedMs, 0)
    }

    func testMarkWatchedZeroMsIsNoopOnWatchedMs() async {
        let agg = FeedAggregator()
        await agg.addItem(makeFeedItem(hash: "vid1"))
        await agg.markWatched(contentHash: "vid1", ms: 0)
        let feed = await agg.getFeed(limit: 1, offset: 0)
        XCTAssertEqual(feed[0].watchedMs, 0)
        // watchCount still increments (intent: the user did open the item)
        XCTAssertEqual(feed[0].watchCount, 1)
    }

    // MARK: - empty feed edge cases

    func testGetFeedOnEmptyAggregatorReturnsEmpty() async {
        let agg = FeedAggregator()
        let feed = await agg.getFeed(limit: 10, offset: 0)
        XCTAssertTrue(feed.isEmpty)
    }

    func testMarkWatchedOnEmptyAggregatorIsNoop() async {
        let agg = FeedAggregator()
        // Must not crash
        await agg.markWatched(contentHash: "anything", ms: 1000)
        XCTAssertEqual(await agg.count, 0)
    }

    // MARK: - thread safety

    func testConcurrentAddsProduceSafeFeed() async {
        let agg = FeedAggregator()
        await withTaskGroup(of: Void.self) { group in
            for i in 0..<100 {
                group.addTask {
                    await agg.addItem(self.makeFeedItem(hash: "t\(i)"))
                }
            }
        }
        let c = await agg.count
        XCTAssertEqual(c, 100)
    }

    func testConcurrentMarkWatchedDoesNotCrash() async {
        let agg = FeedAggregator()
        await agg.addItem(makeFeedItem(hash: "shared"))
        await withTaskGroup(of: Void.self) { group in
            for _ in 0..<50 {
                group.addTask {
                    await agg.markWatched(contentHash: "shared", ms: 100)
                }
            }
        }
        let feed = await agg.getFeed(limit: 1, offset: 0)
        XCTAssertEqual(feed[0].watchCount, 50)
        XCTAssertEqual(feed[0].watchedMs, 5000)
    }
}
