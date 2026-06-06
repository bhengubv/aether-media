#include "aethermesh_forge.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

static void log_not_implemented(const char *fn_name) {
    fprintf(stderr, "[aether-forge] %s: not implemented\n", fn_name);
}

AetherMeshForgeQueryResult *aethermesh_forge_query(
    const char *package_id,
    const char *ecosystem,
    const char *version
) {
    log_not_implemented("aethermesh_forge_query");
    (void)package_id;
    (void)ecosystem;
    (void)version;
    return NULL;
}

int aethermesh_forge_cache(const AetherMeshForgeEntry *entry) {
    log_not_implemented("aethermesh_forge_cache");
    (void)entry;
    return 0;
}

int aethermesh_forge_fetch(
    const char *package_id,
    const char *ecosystem,
    const char *version,
    uint8_t   **out_data,
    size_t     *out_size
) {
    log_not_implemented("aethermesh_forge_fetch");
    (void)package_id;
    (void)ecosystem;
    (void)version;
    if (out_data) *out_data = NULL;
    if (out_size) *out_size = 0;
    return 0;
}

AetherMeshForgeStats *aethermesh_forge_stats(void) {
    log_not_implemented("aethermesh_forge_stats");
    return NULL;
}

void aethermesh_forge_free_entry(AetherMeshForgeEntry *entry) {
    if (!entry) return;
    free(entry->package_id);
    free(entry->ecosystem);
    free(entry->version);
    free(entry->name);
    free(entry->description);
    free(entry->checksum);
    free(entry->download_url);
    free(entry);
}

void aethermesh_forge_free_query(AetherMeshForgeQueryResult *result) {
    if (!result) return;
    aethermesh_forge_free_entry(result->entry);
    free(result);
}

void aethermesh_forge_free_stats(AetherMeshForgeStats *stats) {
    free(stats);
}
