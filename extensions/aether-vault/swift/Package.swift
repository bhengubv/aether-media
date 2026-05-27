// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "AetherVault",
    platforms: [
        .iOS(.v15),
        .macOS(.v12)
    ],
    products: [
        .library(
            name: "AetherVault",
            targets: ["AetherVault"]
        )
    ],
    targets: [
        .target(
            name: "AetherVault",
            path: "Sources/AetherVault"
        ),
        .testTarget(
            name: "AetherVaultTests",
            dependencies: ["AetherVault"],
            path: "Tests/AetherVaultTests"
        )
    ]
)
