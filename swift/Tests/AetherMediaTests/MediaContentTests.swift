import XCTest
@testable import AetherMedia

final class MediaContentTests: XCTestCase {

    // MARK: - Helpers

    private func content(durationMs: Int64, contentType: String = "video/mp4") -> MediaContent {
        MediaContent(
            contentHash:  "abc123",
            title:        "Test",
            durationMs:   durationMs,
            codec:        "h264",
            contentType:  contentType,
            creatorUhid:  "u1",
            sizeBytes:    1_000_000,
            createdAtMs:  0
        )
    }

    // MARK: - formattedDuration

    func testFormattedDurationLiveWhenZero() {
        XCTAssertEqual(content(durationMs: 0).formattedDuration, "Live")
    }

    func testFormattedDurationLiveWhenNegative() {
        XCTAssertEqual(content(durationMs: -1).formattedDuration, "Live")
    }

    func testFormattedDurationSubHour() {
        // 4 minutes 32 seconds = 272 000 ms → "4:32"
        XCTAssertEqual(content(durationMs: 272_000).formattedDuration, "4:32")
    }

    func testFormattedDurationPadsSeconds() {
        // 1 minute 5 seconds = 65 000 ms → "1:05"
        XCTAssertEqual(content(durationMs: 65_000).formattedDuration, "1:05")
    }

    func testFormattedDurationExactlyOneHour() {
        XCTAssertEqual(content(durationMs: 3_600_000).formattedDuration, "1:00:00")
    }

    func testFormattedDurationOverHour() {
        // 1h 23m 45s = 5 025 000 ms → "1:23:45"
        XCTAssertEqual(content(durationMs: 5_025_000).formattedDuration, "1:23:45")
    }

    // MARK: - isVideo / isAudio

    func testIsVideoTrueForVideoMimeType() {
        XCTAssertTrue(content(durationMs: 1000, contentType: "video/mp4").isVideo)
    }

    func testIsVideoFalseForAudioMimeType() {
        XCTAssertFalse(content(durationMs: 1000, contentType: "audio/mp3").isVideo)
    }

    func testIsAudioTrueForAudioMimeType() {
        XCTAssertTrue(content(durationMs: 1000, contentType: "audio/flac").isAudio)
    }

    func testIsAudioFalseForVideoMimeType() {
        XCTAssertFalse(content(durationMs: 1000, contentType: "video/webm").isAudio)
    }

    func testIsVideoCaseInsensitive() {
        XCTAssertTrue(content(durationMs: 1000, contentType: "VIDEO/MP4").isVideo)
    }

    // MARK: - MediaReaction validation

    func testCommentReactionRequiresMessage() {
        XCTAssertThrowsError(
            try MediaReaction(
                reactionId: "r1", contentHash: "h", fromUhid: "u",
                type: .comment, positionMs: 0, message: nil, sentAtMs: 0
            )
        ) { error in
            XCTAssertTrue(error is MediaReactionError)
        }
    }

    func testCommentReactionBlankMessageThrows() {
        XCTAssertThrowsError(
            try MediaReaction(
                reactionId: "r1", contentHash: "h", fromUhid: "u",
                type: .comment, positionMs: 0, message: "   ", sentAtMs: 0
            )
        )
    }

    func testLikeReactionWithMessageThrows() {
        XCTAssertThrowsError(
            try MediaReaction(
                reactionId: "r1", contentHash: "h", fromUhid: "u",
                type: .like, positionMs: 0, message: "oops", sentAtMs: 0
            )
        )
    }

    func testCommentReactionValid() throws {
        let r = try MediaReaction(
            reactionId: "r1", contentHash: "h", fromUhid: "u",
            type: .comment, positionMs: 1500, message: "Great stream!", sentAtMs: 0
        )
        XCTAssertEqual(r.message, "Great stream!")
    }

    func testLikeReactionValid() throws {
        let r = try MediaReaction(
            reactionId: "r2", contentHash: "h", fromUhid: "u",
            type: .like, positionMs: 0, message: nil, sentAtMs: 0
        )
        XCTAssertNil(r.message)
    }
}
