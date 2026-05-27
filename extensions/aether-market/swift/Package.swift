// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "AetherMarket",
    platforms: [
        .iOS(.v15),
        .macOS(.v12)
    ],
    products: [
        .library(
            name: "AetherMarket",
            targets: ["AetherMarket"]
        )
    ],
    targets: [
        .target(
            name: "AetherMarket",
            path: "Sources/AetherMarket"
        ),
        .testTarget(
            name: "AetherMarketTests",
            dependencies: ["AetherMarket"],
            path: "Tests/AetherMarketTests"
        )
    ]
)
