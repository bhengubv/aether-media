cmake_minimum_required(VERSION 3.20)
project(aethernet_market VERSION 1.0.0 LANGUAGES C)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)

add_library(aethernet_market STATIC
    src/market.c
)

target_include_directories(aethernet_market
    PUBLIC
        $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
        $<INSTALL_INTERFACE:include>
)

target_compile_options(aethernet_market PRIVATE
    $<$<C_COMPILER_ID:GNU,Clang>:-Wall -Wextra -Wpedantic>
    $<$<C_COMPILER_ID:MSVC>:/W4>
)

install(TARGETS aethernet_market
    ARCHIVE DESTINATION lib
)
install(FILES include/aethernet_market.h DESTINATION include)
