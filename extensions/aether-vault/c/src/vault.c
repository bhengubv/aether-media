#include "aether_vault.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

static void log_not_implemented(const char *fn_name) {
    fprintf(stderr, "[aether-vault] %s: not implemented\n", fn_name);
}

AetherVaultManifest *aether_vault_store(
    const char    *owner_id,
    const char    *name,
    const uint8_t *data,
    size_t         data_size
) {
    log_not_implemented("aether_vault_store");
    (void)owner_id;
    (void)name;
    (void)data;
    (void)data_size;
    return NULL;
}

int aether_vault_recover(
    const char  *manifest_id,
    const char  *requester_id,
    uint8_t    **out_data,
    size_t      *out_size
) {
    log_not_implemented("aether_vault_recover");
    (void)manifest_id;
    (void)requester_id;
    if (out_data) *out_data = NULL;
    if (out_size) *out_size = 0;
    return 0;
}

AetherVaultHealth *aether_vault_health(const char *manifest_id) {
    log_not_implemented("aether_vault_health");
    (void)manifest_id;
    return NULL;
}

int aether_vault_delete(const char *manifest_id, const char *requester_id) {
    log_not_implemented("aether_vault_delete");
    (void)manifest_id;
    (void)requester_id;
    return 0;
}

void aether_vault_free_manifest(AetherVaultManifest *manifest) {
    if (!manifest) return;
    free(manifest->name);
    free(manifest->description);
    free(manifest);
}

void aether_vault_free_health(AetherVaultHealth *health) {
    free(health);
}
