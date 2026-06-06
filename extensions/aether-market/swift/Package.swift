// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "AetherNetMarket",
    platforms: [
        .iOS(.v15),
        .macOS(.v12)
    ],
    products: [
        .library(
            name: "AetherNetMarket",
            targets: ["AetherNetMarket"]
        )
    ],
    targets: [
        .target(
            name: "AetherNetMarket",
            path: "Sources/AetherNetMarket"
        ),
        .testTarget(
            name: "AetherNetMarketTests",
            dependencies: ["AetherNetMarket"],
            path: "Tests/AetherNetMarketTests"
        )
    ]
)
