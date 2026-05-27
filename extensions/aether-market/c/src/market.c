#include "aether_market.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

static void log_not_implemented(const char *fn_name) {
    fprintf(stderr, "[aether-market] %s: not implemented\n", fn_name);
}

/* ── PoV stubs ──────────────────────────────────────────────────────────────── */

AetherPoVToken *aether_pov_issue(const AetherPoVToken *token) {
    log_not_implemented("aether_pov_issue");
    (void)token;
    return NULL;
}

int aether_pov_revoke(const char *token_id, const char *reason) {
    log_not_implemented("aether_pov_revoke");
    (void)token_id;
    (void)reason;
    return 0;
}

AetherPoVScore *aether_pov_get_score(const char *subject_id) {
    log_not_implemented("aether_pov_get_score");
    (void)subject_id;
    return NULL;
}

int aether_pov_verify(const char *token_id) {
    log_not_implemented("aether_pov_verify");
    (void)token_id;
    return 0;
}

void aether_pov_free_token(AetherPoVToken *token) {
    if (!token) return;
    free(token->context);
    free(token->claim);
    free(token->evidence);
    free(token->signature);
    free(token);
}

void aether_pov_free_score(AetherPoVScore *score) {
    free(score);
}

/* ── Market stubs ───────────────────────────────────────────────────────────── */

AetherMarketListing *aether_market_create_listing(const AetherMarketListing *listing) {
    log_not_implemented("aether_market_create_listing");
    (void)listing;
    return NULL;
}

AetherMarketListing *aether_market_get_listing(const char *listing_id) {
    log_not_implemented("aether_market_get_listing");
    (void)listing_id;
    return NULL;
}

int aether_market_delete_listing(const char *listing_id, const char *requester_id) {
    log_not_implemented("aether_market_delete_listing");
    (void)listing_id;
    (void)requester_id;
    return 0;
}

AetherTradeEscrow *aether_market_initiate_escrow(
    const char *listing_id,
    const char *buyer_id
) {
    log_not_implemented("aether_market_initiate_escrow");
    (void)listing_id;
    (void)buyer_id;
    return NULL;
}

AetherTradeEscrow *aether_market_advance_escrow(
    const char      *escrow_id,
    AetherTradeState  new_state,
    const char      *actor_id,
    const char      *notes
) {
    log_not_implemented("aether_market_advance_escrow");
    (void)escrow_id;
    (void)new_state;
    (void)actor_id;
    (void)notes;
    return NULL;
}

void aether_market_free_listing(AetherMarketListing *listing) {
    if (!listing) return;
    free(listing->title);
    free(listing->description);
    free(listing);
}

void aether_market_free_escrow(AetherTradeEscrow *escrow) {
    if (!escrow) return;
    free(escrow->description);
    free(escrow->dispute_reason);
    free(escrow->resolution_notes);
    free(escrow);
}
