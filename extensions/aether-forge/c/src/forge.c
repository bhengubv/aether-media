#include "aethernet_forge.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

static void log_not_implemented(const char *fn_name) {
    fprintf(stderr, "[aether-forge] %s: not implemented\n", fn_name);
}

AetherNetForgeQueryResult *aethernet_forge_query(
    const char *package_id,
    const char *ecosystem,
    const char *version
) {
    log_not_implemented("aethernet_forge_query");
    (void)package_id;
    (void)ecosystem;
    (void)version;
    return NULL;
}

int aethernet_forge_cache(const AetherNetForgeEntry *entry) {
    log_not_implemented("aethernet_forge_cache");
    (void)entry;
    return 0;
}

int aethernet_forge_fetch(
    const char *package_id,
    const char *ecosystem,
    const char *version,
    uint8_t   **out_data,
    size_t     *out_size
) {
    log_not_implemented("aethernet_forge_fetch");
    (void)package_id;
    (void)ecosystem;
    (void)version;
    if (out_data) *out_data = NULL;
    if (out_size) *out_size = 0;
    return 0;
}

AetherNetForgeStats *aethernet_forge_stats(void) {
    log_not_implemented("aethernet_forge_stats");
    return NULL;
}

void aethernet_forge_free_entry(AetherNetForgeEntry *entry) {
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

void aethernet_forge_free_query(AetherNetForgeQueryResult *result) {
    if (!result) return;
    aethernet_forge_free_entry(result->entry);
    free(result);
}

void aethernet_forge_free_stats(AetherNetForgeStats *stats) {
    free(stats);
}
