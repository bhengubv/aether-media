#include "aethernet_market.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

static void log_not_implemented(const char *fn_name) {
    fprintf(stderr, "[aether-market] %s: not implemented\n", fn_name);
}

/* ── PoV stubs ──────────────────────────────────────────────────────────────── */

AetherNetPoVToken *aethernet_pov_issue(const AetherNetPoVToken *token) {
    log_not_implemented("aethernet_pov_issue");
    (void)token;
    return NULL;
}

int aethernet_pov_revoke(const char *token_id, const char *reason) {
    log_not_implemented("aethernet_pov_revoke");
    (void)token_id;
    (void)reason;
    return 0;
}

AetherNetPoVScore *aethernet_pov_get_score(const char *subject_id) {
    log_not_implemented("aethernet_pov_get_score");
    (void)subject_id;
    return NULL;
}

int aethernet_pov_verify(const char *token_id) {
    log_not_implemented("aethernet_pov_verify");
    (void)token_id;
    return 0;
}

void aethernet_pov_free_token(AetherNetPoVToken *token) {
    if (!token) return;
    free(token->context);
    free(token->claim);
    free(token->evidence);
    free(token->signature);
    free(token);
}

void aethernet_pov_free_score(AetherNetPoVScore *score) {
    free(score);
}

/* ── Market stubs ───────────────────────────────────────────────────────────── */

AetherNetMarketListing *aethernet_market_create_listing(const AetherNetMarketListing *listing) {
    log_not_implemented("aethernet_market_create_listing");
    (void)listing;
    return NULL;
}

AetherNetMarketListing *aethernet_market_get_listing(const char *listing_id) {
    log_not_implemented("aethernet_market_get_listing");
    (void)listing_id;
    return NULL;
}

int aethernet_market_delete_listing(const char *listing_id, const char *requester_id) {
    log_not_implemented("aethernet_market_delete_listing");
    (void)listing_id;
    (void)requester_id;
    return 0;
}

AetherNetTradeEscrow *aethernet_market_initiate_escrow(
    const char *listing_id,
    const char *buyer_id
) {
    log_not_implemented("aethernet_market_initiate_escrow");
    (void)listing_id;
    (void)buyer_id;
    return NULL;
}

AetherNetTradeEscrow *aethernet_market_advance_escrow(
    const char      *escrow_id,
    AetherNetTradeState  new_state,
    const char      *actor_id,
    const char      *notes
) {
    log_not_implemented("aethernet_market_advance_escrow");
    (void)escrow_id;
    (void)new_state;
    (void)actor_id;
    (void)notes;
    return NULL;
}

void aethernet_market_free_listing(AetherNetMarketListing *listing) {
    if (!listing) return;
    free(listing->title);
    free(listing->description);
    free(listing);
}

void aethernet_market_free_escrow(AetherNetTradeEscrow *escrow) {
    if (!escrow) return;
    free(escrow->description);
    free(escrow->dispute_reason);
    free(escrow->resolution_notes);
    free(escrow);
}
