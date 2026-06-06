// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "AetherNetSpace",
    platforms: [
        .iOS(.v15),
        .macOS(.v12)
    ],
    products: [
        .library(
            name: "AetherNetSpace",
            targets: ["AetherNetSpace"]
        )
    ],
    targets: [
        .target(
            name: "AetherNetSpace",
            path: "Sources/AetherNetSpace"
        ),
        .testTarget(
            name: "AetherNetSpaceTests",
            dependencies: ["AetherNetSpace"],
            path: "Tests/AetherNetSpaceTests"
        )
    ]
)
