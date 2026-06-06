#ifndef AETHERNET_VAULT_H
#define AETHERNET_VAULT_H

#include <stdint.h>
#include <stddef.h>
#include <time.h>

#ifdef __cplusplus
extern "C" {
#endif

/* ── Structs ────────────────────────────────────────────────────────────────── */

/**
 * AetherNetVaultManifest
 * Metadata for an erasure-coded, encrypted distributed backup.
 */
typedef struct AetherNetVaultManifest {
    char     id[37];                    /* UUID string */
    char     owner_id[37];
    char    *name;                      /* heap-allocated */
    char    *description;               /* heap-allocated, may be NULL */
    int64_t  original_size_bytes;
    int64_t  encoded_size_bytes;
    int      shard_count;
    int      parity_shard_count;
    int      min_shards_for_recovery;
    char     checksum[128];
    char     checksum_algorithm[16];
    char     encryption_algorithm[32];
    int      replication_factor;
    time_t   created_at;
    time_t   updated_at;
    time_t   expires_at;               /* 0 = no expiry */
} AetherNetVaultManifest;

/**
 * AetherNetVaultHealth
 * Current recoverability and availability status of a stored object.
 */
typedef struct AetherNetVaultHealth {
    char   manifest_id[37];
    int    total_shards;
    int    available_shards;
    int    parity_shards;
    int    available_parity_shards;
    int    min_shards_for_recovery;
    int    replication_factor;
    int    is_recoverable;   /* boolean: 1 = can reconstruct, 0 = lost */
    int    is_healthy;       /* boolean: 1 = all shards present */
    double health_percent;
    time_t last_checked_at;
} AetherNetVaultHealth;

/* ── Function Prototypes ────────────────────────────────────────────────────── */

/**
 * Encode, encrypt, and distribute data across mesh nodes.
 * Returns a heap-allocated manifest on success, NULL on failure.
 * Caller frees with aethernet_vault_free_manifest().
 */
AetherNetVaultManifest *aethernet_vault_store(
    const char    *owner_id,
    const char    *name,
    const uint8_t *data,
    size_t         data_size
);

/**
 * Reconstruct and decrypt original data from available shards.
 * On success, *out_data is set to a heap-allocated buffer and *out_size to its length.
 * Caller must free(*out_data).
 * Returns 1 on success, 0 on failure.
 */
int aethernet_vault_recover(
    const char  *manifest_id,
    const char  *requester_id,
    uint8_t    **out_data,
    size_t      *out_size
);

/**
 * Return shard availability and recoverability for a manifest.
 * Returns a heap-allocated health struct; caller frees with aethernet_vault_free_health().
 */
AetherNetVaultHealth *aethernet_vault_health(const char *manifest_id);

/**
 * Instruct all nodes to drop their shards for manifest_id.
 * Returns 1 if deleted, 0 otherwise.
 */
int aethernet_vault_delete(const char *manifest_id, const char *requester_id);

/**
 * Free a heap-allocated AetherNetVaultManifest.
 */
void aethernet_vault_free_manifest(AetherNetVaultManifest *manifest);

/**
 * Free a heap-allocated AetherNetVaultHealth.
 */
void aethernet_vault_free_health(AetherNetVaultHealth *health);

#ifdef __cplusplus
}
#endif

#endif /* AETHERNET_VAULT_H */
