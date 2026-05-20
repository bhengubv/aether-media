// swift-tools-version:5.9
import PackageDescription

let package = Package(
    name: "AetherMedia",
    platforms: [
        .iOS(.v16),
        .macOS(.v13),
    ],
    products: [
        .library(
            name: "AetherMedia",
            targets: ["AetherMedia"]
        ),
    ],
    dependencies: [
        // Aether Protocol mesh transport layer
        // .package(url: "https://github.com/bhengubv/aether-protocol", from: "1.0.0"),
    ],
    targets: [
        .target(
            name: "AetherMedia",
            dependencies: [
                // .product(name: "AetherProtocol", package: "aether-protocol"),
            ],
            path: "Sources/AetherMedia"
        ),
        .testTarget(
            name: "AetherMediaTests",
            dependencies: ["AetherMedia"],
            path: "Tests/AetherMediaTests"
        ),
    ]
)
