#ifndef AETHERNET_FORGE_H
#define AETHERNET_FORGE_H

#include <stdint.h>
#include <stddef.h>
#include <time.h>

#ifdef __cplusplus
extern "C" {
#endif

/* ── Structs ────────────────────────────────────────────────────────────────── */

/**
 * AetherNetForgeEntry
 * Represents a cached package in the mesh-local package proxy.
 */
typedef struct AetherNetForgeEntry {
    char     id[37];                 /* UUID string */
    char    *package_id;             /* heap-allocated */
    char    *ecosystem;              /* e.g. "pypi", "npm", "maven" */
    char    *version;
    char    *name;
    char    *description;            /* may be NULL */
    char    *checksum;
    char     checksum_algorithm[16]; /* e.g. "sha256" */
    char    *download_url;
    int64_t  size_bytes;
    int64_t  download_count;
    int      is_verified;
    time_t   cached_at;
    time_t   expires_at;             /* 0 = no expiry */
} AetherNetForgeEntry;

/**
 * AetherNetForgeStats
 * Aggregate cache statistics.
 */
typedef struct AetherNetForgeStats {
    int64_t total_entries;
    int64_t total_size_bytes;
    int64_t total_downloads;
    int     unique_ecosystems;
    int64_t verified_packages;
    double  hit_rate;
    double  miss_rate;
    int     active_peers;
    time_t  last_updated;
} AetherNetForgeStats;

/**
 * AetherNetForgeQueryResult
 * Returned by aethernet_forge_query.
 */
typedef struct AetherNetForgeQueryResult {
    AetherNetForgeEntry *entry;   /* NULL if not found */
    int               found;   /* 1 if entry is valid, 0 otherwise */
} AetherNetForgeQueryResult;

/* ── Function Prototypes ────────────────────────────────────────────────────── */

/**
 * Look up a package in the mesh cache.
 * Returns a heap-allocated query result; caller frees with aethernet_forge_free_query().
 * Pass NULL for version to match any version.
 */
AetherNetForgeQueryResult *aethernet_forge_query(
    const char *package_id,
    const char *ecosystem,
    const char *version
);

/**
 * Store a package entry in the mesh cache.
 * Returns 1 on success, 0 on failure.
 */
int aethernet_forge_cache(const AetherNetForgeEntry *entry);

/**
 * Download raw package bytes.
 * On success, *out_data is set to a heap-allocated buffer and *out_size to its length.
 * Caller must free(*out_data).
 * Returns 1 on success, 0 on failure.
 */
int aethernet_forge_fetch(
    const char *package_id,
    const char *ecosystem,
    const char *version,
    uint8_t   **out_data,
    size_t     *out_size
);

/**
 * Retrieve aggregate cache statistics.
 * Returns a heap-allocated stats struct; caller frees with aethernet_forge_free_stats().
 */
AetherNetForgeStats *aethernet_forge_stats(void);

/**
 * Free a heap-allocated AetherNetForgeEntry.
 */
void aethernet_forge_free_entry(AetherNetForgeEntry *entry);

/**
 * Free a heap-allocated AetherNetForgeQueryResult.
 */
void aethernet_forge_free_query(AetherNetForgeQueryResult *result);

/**
 * Free a heap-allocated AetherNetForgeStats.
 */
void aethernet_forge_free_stats(AetherNetForgeStats *stats);

#ifdef __cplusplus
}
#endif

#endif /* AETHERNET_FORGE_H */
