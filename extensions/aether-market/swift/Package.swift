// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "AetherMeshMarket",
    platforms: [
        .iOS(.v15),
        .macOS(.v12)
    ],
    products: [
        .library(
            name: "AetherMeshMarket",
            targets: ["AetherMeshMarket"]
        )
    ],
    targets: [
        .target(
            name: "AetherMeshMarket",
            path: "Sources/AetherMeshMarket"
        ),
        .testTarget(
            name: "AetherMeshMarketTests",
            dependencies: ["AetherMeshMarket"],
            path: "Tests/AetherMeshMarketTests"
        )
    ]
)
