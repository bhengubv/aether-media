// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "AetherNetVault",
    platforms: [
        .iOS(.v15),
        .macOS(.v12)
    ],
    products: [
        .library(
            name: "AetherNetVault",
            targets: ["AetherNetVault"]
        )
    ],
    targets: [
        .target(
            name: "AetherNetVault",
            path: "Sources/AetherNetVault"
        ),
        .testTarget(
            name: "AetherNetVaultTests",
            dependencies: ["AetherNetVault"],
            path: "Tests/AetherNetVaultTests"
        )
    ]
)
