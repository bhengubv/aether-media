cmake_minimum_required(VERSION 3.20)
project(aether_market VERSION 1.0.0 LANGUAGES C)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)

add_library(aether_market STATIC
    src/market.c
)

target_include_directories(aether_market
    PUBLIC
        $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
        $<INSTALL_INTERFACE:include>
)

target_compile_options(aether_market PRIVATE
    $<$<C_COMPILER_ID:GNU,Clang>:-Wall -Wextra -Wpedantic>
    $<$<C_COMPILER_ID:MSVC>:/W4>
)

install(TARGETS aether_market
    ARCHIVE DESTINATION lib
)
install(FILES include/aether_market.h DESTINATION include)
