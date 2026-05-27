#include "aether_space.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

/* ── Internal helpers ──────────────────────────────────────────────────────── */

static void log_not_implemented(const char *fn_name) {
    fprintf(stderr, "[aether-space] %s: not implemented\n", fn_name);
}

static AetherSpaceBreadcrumb *copy_breadcrumb(const AetherSpaceBreadcrumb *src) {
    if (!src) return NULL;
    AetherSpaceBreadcrumb *dst = (AetherSpaceBreadcrumb *)calloc(1, sizeof(AetherSpaceBreadcrumb));
    if (!dst) return NULL;
    memcpy(dst, src, sizeof(AetherSpaceBreadcrumb));
    /* deep-copy heap strings */
    dst->title = src->title ? strdup(src->title) : NULL;
    dst->body  = src->body  ? strdup(src->body)  : NULL;
    return dst;
}

/* ── Public API stubs ──────────────────────────────────────────────────────── */

AetherSpaceBreadcrumb *aether_space_drop(const AetherSpaceBreadcrumb *breadcrumb) {
    log_not_implemented("aether_space_drop");
    (void)breadcrumb;
    return NULL;
}

AetherSpaceScanResult *aether_space_scan(const char *geo_hash, double radius_km) {
    log_not_implemented("aether_space_scan");
    (void)geo_hash;
    (void)radius_km;
    return NULL;
}

AetherSpaceBreadcrumb *aether_space_pin(const char *breadcrumb_id, const char *space_id) {
    log_not_implemented("aether_space_pin");
    (void)breadcrumb_id;
    (void)space_id;
    return NULL;
}

int aether_space_delete(const char *breadcrumb_id, const char *requester_id) {
    log_not_implemented("aether_space_delete");
    (void)breadcrumb_id;
    (void)requester_id;
    return 0;
}

void aether_space_free(AetherSpaceBreadcrumb *breadcrumb) {
    if (!breadcrumb) return;
    free(breadcrumb->title);
    free(breadcrumb->body);
    free(breadcrumb);
}

void aether_space_free_scan(AetherSpaceScanResult *result) {
    if (!result) return;
    for (size_t i = 0; i < result->count; ++i) {
        free(result->items[i].title);
        free(result->items[i].body);
    }
    free(result->items);
    free(result);
}
