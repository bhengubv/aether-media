plugins {
    kotlin("jvm") version "1.9.22"
    kotlin("plugin.serialization") version "1.9.22"
    `maven-publish`
}

group = "dev.aethernet"
version = "1.0.0"

repositories {
    mavenCentral()
}

dependencies {
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-core:1.7.3")
    implementation("org.jetbrains.kotlinx:kotlinx-serialization-json:1.6.2")
    implementation(kotlin("stdlib"))

    // Note: media3-exoplayer is Android-only.  For JVM tests we use the
    // core models and social logic only.  ExoPlayer integration lives in
    // the Android module (android/).
    testImplementation("org.junit.jupiter:junit-jupiter:5.10.1")
    testRuntimeOnly("org.junit.platform:junit-platform-launcher")
}

tasks.test {
    useJUnitPlatform()
}

// ── Cross-language wire-roundtrip ────────────────────────────────────────────
//
// Drives the Kotlin lane of `tests/cross-language/run_all.sh`. Reads the three
// golden JSON fixtures via kotlinx.serialization and emits MODEL:JSON lines.
// The harness compares the output against the goldens to prove wire-format
// identity with the C# reference implementation (and Go / Python / TS /
// Rust / Swift / C).
tasks.register<JavaExec>("wireRoundtrip") {
    group = "verification"
    description = "Round-trips the 3 golden wire fixtures through the Kotlin models."
    classpath = sourceSets["main"].runtimeClasspath
    mainClass.set("aethermedia.WireRoundtripKt")
    standardOutput = System.out
}

publishing {
    repositories {
        maven {
            name = "GitHubPackages"
            url = uri("https://maven.pkg.github.com/bhengubv/aether-media")
            credentials {
                username = System.getenv("GITHUB_ACTOR") ?: ""
                password = System.getenv("GITHUB_TOKEN") ?: ""
            }
        }
    }
    publications {
        create<MavenPublication>("mavenJava") {
            from(components["java"])
            groupId    = "dev.aethernet"
            artifactId = "aethernet-media"
            version    = project.version.toString()
        }
    }
}
