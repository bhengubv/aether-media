cmake_minimum_required(VERSION 3.20)
project(aethermesh_vault VERSION 1.0.0 LANGUAGES C)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)

add_library(aethermesh_vault STATIC
    src/vault.c
)

target_include_directories(aethermesh_vault
    PUBLIC
        $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
        $<INSTALL_INTERFACE:include>
)

target_compile_options(aethermesh_vault PRIVATE
    $<$<C_COMPILER_ID:GNU,Clang>:-Wall -Wextra -Wpedantic>
    $<$<C_COMPILER_ID:MSVC>:/W4>
)

install(TARGETS aethermesh_vault
    ARCHIVE DESTINATION lib
)
install(FILES include/aethermesh_vault.h DESTINATION include)
