import XCTest
@testable import AetherMedia

final class SocialGraphTests: XCTestCase {

    // MARK: - count

    func testCountStartsAtZero() async {
        let g = SocialGraph()
        let c = await g.count
        XCTAssertEqual(c, 0)
    }

    // MARK: - follow

    func testFollowAddsUhid() async {
        let g = SocialGraph()
        await g.follow("alice")
        let following = await g.isFollowing("alice")
        XCTAssertTrue(following)
    }

    func testFollowMultipleAccounts() async {
        let g = SocialGraph()
        await g.follow("alice")
        await g.follow("bob")
        let c = await g.count
        XCTAssertEqual(c, 2)
        XCTAssertTrue(await g.isFollowing("alice"))
        XCTAssertTrue(await g.isFollowing("bob"))
    }

    func testDoubleFollowIsIdempotent() async {
        let g = SocialGraph()
        await g.follow("alice")
        await g.follow("alice")
        let c = await g.count
        XCTAssertEqual(c, 1)
    }

    func testFollowBlankUhidIsIgnored() async {
        let g = SocialGraph()
        await g.follow("   ")
        let c = await g.count
        XCTAssertEqual(c, 0)
    }

    func testFollowEmptyStringIsIgnored() async {
        let g = SocialGraph()
        await g.follow("")
        let c = await g.count
        XCTAssertEqual(c, 0)
    }

    // MARK: - unfollow

    func testUnfollowRemovesUhid() async {
        let g = SocialGraph()
        await g.follow("alice")
        await g.unfollow("alice")
        XCTAssertFalse(await g.isFollowing("alice"))
        XCTAssertEqual(await g.count, 0)
    }

    func testUnfollowNonFollowingIsNoop() async {
        let g = SocialGraph()
        // Must not crash
        await g.unfollow("ghost")
        XCTAssertEqual(await g.count, 0)
    }

    func testUnfollowOnlyTargetedUhid() async {
        let g = SocialGraph()
        await g.follow("alice")
        await g.follow("bob")
        await g.unfollow("alice")
        XCTAssertFalse(await g.isFollowing("alice"))
        XCTAssertTrue(await g.isFollowing("bob"))
        XCTAssertEqual(await g.count, 1)
    }

    // MARK: - isFollowing

    func testIsFollowingReturnsFalseForUnknown() async {
        let g = SocialGraph()
        XCTAssertFalse(await g.isFollowing("nobody"))
    }

    func testIsFollowingReturnsTrueAfterFollow() async {
        let g = SocialGraph()
        await g.follow("charlie")
        XCTAssertTrue(await g.isFollowing("charlie"))
    }

    func testIsFollowingReturnsFalseAfterUnfollow() async {
        let g = SocialGraph()
        await g.follow("dave")
        await g.unfollow("dave")
        XCTAssertFalse(await g.isFollowing("dave"))
    }

    // MARK: - followingList

    func testFollowingListIsEmpty() async {
        let g = SocialGraph()
        let list = await g.followingList()
        XCTAssertTrue(list.isEmpty)
    }

    func testFollowingListIsSorted() async {
        let g = SocialGraph()
        await g.follow("charlie")
        await g.follow("alice")
        await g.follow("bob")
        let list = await g.followingList()
        XCTAssertEqual(list, ["alice", "bob", "charlie"])
    }

    func testFollowingListReflectsUnfollow() async {
        let g = SocialGraph()
        await g.follow("alice")
        await g.follow("bob")
        await g.unfollow("alice")
        let list = await g.followingList()
        XCTAssertEqual(list, ["bob"])
    }

    func testFollowingListSingleItem() async {
        let g = SocialGraph()
        await g.follow("solo")
        let list = await g.followingList()
        XCTAssertEqual(list, ["solo"])
    }

    // MARK: - edge cases

    func testEmptyGraphFollowingListIsEmpty() async {
        let g = SocialGraph()
        let list = await g.followingList()
        XCTAssertEqual(list, [])
    }

    func testFollowAfterUnfollowReAdds() async {
        let g = SocialGraph()
        await g.follow("alice")
        await g.unfollow("alice")
        await g.follow("alice")
        XCTAssertTrue(await g.isFollowing("alice"))
        XCTAssertEqual(await g.count, 1)
    }

    // MARK: - thread safety

    func testConcurrentFollowsProduceSafeCount() async {
        let g = SocialGraph()
        await withTaskGroup(of: Void.self) { group in
            for i in 0..<100 {
                group.addTask {
                    await g.follow("user\(i)")
                }
            }
        }
        let c = await g.count
        XCTAssertEqual(c, 100)
    }

    func testConcurrentFollowUnfollowDoesNotCrash() async {
        let g = SocialGraph()
        await withTaskGroup(of: Void.self) { group in
            for i in 0..<50 {
                group.addTask { await g.follow("user\(i)") }
                group.addTask { await g.unfollow("user\(i)") }
            }
        }
        // Result is non-deterministic but must not crash; count is 0..50
        let c = await g.count
        XCTAssertTrue(c >= 0 && c <= 50)
    }
}
