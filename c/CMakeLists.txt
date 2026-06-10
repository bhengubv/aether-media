cmake_minimum_required(VERSION 3.16)
project(aethermedia VERSION 1.0.0 LANGUAGES C)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)

# ── Library ────────────────────────────────────────────────────────────────────
add_library(aethermedia STATIC
    src/player/vlc_player.c
    src/streaming/stream_client.c
    src/social/social_graph.c
)
target_include_directories(aethermedia PUBLIC include)

# LibVLC (optional — graceful fallback if not found)
find_package(PkgConfig)
if(PkgConfig_FOUND)
    pkg_check_modules(LIBVLC libvlc)
    if(LIBVLC_FOUND)
        message(STATUS "LibVLC found: ${LIBVLC_LIBRARIES}")
        target_compile_definitions(aethermedia PRIVATE AETHERNET_HAVE_LIBVLC=1)
        target_link_libraries(aethermedia PUBLIC ${LIBVLC_LIBRARIES})
        target_include_directories(aethermedia PRIVATE ${LIBVLC_INCLUDE_DIRS})
    else()
        message(WARNING "LibVLC not found — vlc_player will compile as a stub-free no-op")
        target_compile_definitions(aethermedia PRIVATE AETHERNET_HAVE_LIBVLC=0)
    endif()
else()
    target_compile_definitions(aethermedia PRIVATE AETHERNET_HAVE_LIBVLC=0)
endif()

# ── Demo executable ────────────────────────────────────────────────────────────
add_executable(aethermedia_demo src/main.c)
target_link_libraries(aethermedia_demo aethermedia)

# ── Tests ──────────────────────────────────────────────────────────────────────
enable_testing()
add_executable(test_social tests/test_social.c)
target_link_libraries(test_social aethermedia)
add_test(NAME social_graph COMMAND test_social)

# ── Cross-language wire-roundtrip driver ──────────────────────────────────────
#
# Reads the 3 golden JSON fixtures from tests/cross-language/golden/, round-trips
# them through the C struct definitions in include/aethermedia.h, and prints
# MODEL:JSON to stdout. The harness (tests/cross-language/run_all.sh) compares
# the output against the goldens to prove the C SDK's wire format is byte-equal
# to the other 7 language SDKs.
#
# Uses jsmn (vendored under third_party/) — single-header public-domain JSON
# parser, ~400 LOC, no transitive deps. Toggleable so the default `cmake -S .`
# invocation stays lean for downstream consumers who don't need it.
option(BUILD_WIRE_ROUNDTRIP "Build the cross-language wire-roundtrip driver" ON)
if(BUILD_WIRE_ROUNDTRIP)
    add_executable(wire_roundtrip src/wire_roundtrip.c)
    target_link_libraries(wire_roundtrip aethermedia)
    target_include_directories(wire_roundtrip PRIVATE third_party/jsmn)
endif()
