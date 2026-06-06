#ifndef AETHERNET_MARKET_H
#define AETHERNET_MARKET_H

#include <stdint.h>
#include <stddef.h>
#include <time.h>

#ifdef __cplusplus
extern "C" {
#endif

/* ── Enums ─────────────────────────────────────────────────────────────────── */

typedef enum AetherNetPoVTransport {
    AETHERNET_POV_TRANSPORT_MESH        = 0,
    AETHERNET_POV_TRANSPORT_BLUETOOTH   = 1,
    AETHERNET_POV_TRANSPORT_NFC         = 2,
    AETHERNET_POV_TRANSPORT_QR_CODE     = 3,
    AETHERNET_POV_TRANSPORT_DIRECT_LINK = 4
} AetherNetPoVTransport;

typedef enum AetherNetMarketCategory {
    AETHERNET_MARKET_GOODS     = 0,
    AETHERNET_MARKET_SERVICES  = 1,
    AETHERNET_MARKET_DIGITAL   = 2,
    AETHERNET_MARKET_FOOD      = 3,
    AETHERNET_MARKET_TRANSPORT = 4,
    AETHERNET_MARKET_HOUSING   = 5,
    AETHERNET_MARKET_LABOUR    = 6,
    AETHERNET_MARKET_SKILLS    = 7,
    AETHERNET_MARKET_BARTER    = 8,
    AETHERNET_MARKET_OTHER     = 9
} AetherNetMarketCategory;

typedef enum AetherNetTradeState {
    AETHERNET_TRADE_INITIATED      = 0,
    AETHERNET_TRADE_FUNDED         = 1,
    AETHERNET_TRADE_GOODS_SENT     = 2,
    AETHERNET_TRADE_GOODS_RECEIVED = 3,
    AETHERNET_TRADE_DISPUTED       = 4,
    AETHERNET_TRADE_RESOLVED       = 5,
    AETHERNET_TRADE_CANCELLED      = 6,
    AETHERNET_TRADE_EXPIRED        = 7,
    AETHERNET_TRADE_COMPLETED      = 8
} AetherNetTradeState;

typedef enum AetherNetTradeRole {
    AETHERNET_TRADE_ROLE_BUYER   = 0,
    AETHERNET_TRADE_ROLE_SELLER  = 1,
    AETHERNET_TRADE_ROLE_ARBITER = 2
} AetherNetTradeRole;

/* ── Structs ────────────────────────────────────────────────────────────────── */

/**
 * AetherNetPoVToken
 * Proof-of-Value attestation token issued on the mesh.
 */
typedef struct AetherNetPoVToken {
    char              id[37];
    char              issuer_id[37];
    char              subject_id[37];
    char             *context;          /* heap-allocated */
    char             *claim;            /* heap-allocated */
    char             *evidence;         /* heap-allocated, may be NULL */
    char             *signature;        /* heap-allocated */
    AetherNetPoVTransport transport;
    double            weight;
    int               is_revoked;
    time_t            issued_at;
    time_t            expires_at;       /* 0 = no expiry */
} AetherNetPoVToken;

/**
 * AetherNetPoVScore
 * Aggregated trust score for a user.
 */
typedef struct AetherNetPoVScore {
    char   subject_id[37];
    double overall_score;
    double trade_score;
    double reliability_score;
    double response_score;
    int    token_count;
    int    positive_tokens;
    int    negative_tokens;
    int    successful_trades;
    int    failed_trades;
    char   level[32];
    time_t last_updated;
} AetherNetPoVScore;

/**
 * AetherNetMarketListing
 * A product or service offered on the mesh marketplace.
 */
typedef struct AetherNetMarketListing {
    char                 id[37];
    char                 seller_id[37];
    char                 space_id[37];    /* all-zero if not set */
    char                 geo_hash[13];
    AetherNetMarketCategory category;
    char                *title;           /* heap-allocated */
    char                *description;     /* heap-allocated */
    double               price_amount;
    char                 price_currency[8];
    int                  accepts_barter;
    int                  is_available;
    int                  quantity;
    int                  requires_escrow;
    double               minimum_pov_score;
    time_t               created_at;
    time_t               updated_at;
    time_t               expires_at;      /* 0 = no expiry */
} AetherNetMarketListing;

/**
 * AetherNetTradeEscrow
 * Mesh-native trade escrow record.
 */
typedef struct AetherNetTradeEscrow {
    char            id[37];
    char            listing_id[37];
    char            buyer_id[37];
    char            seller_id[37];
    char            arbiter_id[37];    /* all-zero if not set */
    AetherNetTradeState state;
    double           amount;
    char             currency[8];
    char            *description;      /* heap-allocated, may be NULL */
    int              buyer_confirmed;
    int              seller_confirmed;
    char            *dispute_reason;   /* heap-allocated, may be NULL */
    char            *resolution_notes; /* heap-allocated, may be NULL */
    int              timeout_hours;
    time_t           created_at;
    time_t           updated_at;
    time_t           completed_at;     /* 0 = not yet completed */
    time_t           expires_at;       /* 0 = no expiry */
} AetherNetTradeEscrow;

/* ── PoV Function Prototypes ────────────────────────────────────────────────── */

/**
 * Issue a PoV token on the mesh.
 * Returns a heap-allocated token on success, NULL on failure.
 * Caller frees with aethernet_pov_free_token().
 */
AetherNetPoVToken *aethernet_pov_issue(const AetherNetPoVToken *token);

/**
 * Revoke a PoV token by ID.
 * Returns 1 on success, 0 otherwise.
 */
int aethernet_pov_revoke(const char *token_id, const char *reason);

/**
 * Get the aggregated trust score for a user.
 * Returns a heap-allocated score; caller frees with aethernet_pov_free_score().
 */
AetherNetPoVScore *aethernet_pov_get_score(const char *subject_id);

/**
 * Verify a token's cryptographic signature.
 * Returns 1 if valid, 0 otherwise.
 */
int aethernet_pov_verify(const char *token_id);

/**
 * Free a heap-allocated AetherNetPoVToken.
 */
void aethernet_pov_free_token(AetherNetPoVToken *token);

/**
 * Free a heap-allocated AetherNetPoVScore.
 */
void aethernet_pov_free_score(AetherNetPoVScore *score);

/* ── Market Function Prototypes ─────────────────────────────────────────────── */

/**
 * Publish a market listing to the mesh.
 * Returns a heap-allocated listing on success, NULL on failure.
 * Caller frees with aethernet_market_free_listing().
 */
AetherNetMarketListing *aethernet_market_create_listing(const AetherNetMarketListing *listing);

/**
 * Retrieve a listing by ID.
 * Returns heap-allocated listing or NULL if not found.
 */
AetherNetMarketListing *aethernet_market_get_listing(const char *listing_id);

/**
 * Delete a listing.
 * Returns 1 on success, 0 otherwise.
 */
int aethernet_market_delete_listing(const char *listing_id, const char *requester_id);

/**
 * Initiate an escrow for a trade.
 * Returns heap-allocated escrow on success, NULL on failure.
 * Caller frees with aethernet_market_free_escrow().
 */
AetherNetTradeEscrow *aethernet_market_initiate_escrow(
    const char *listing_id,
    const char *buyer_id
);

/**
 * Advance an escrow to a new state.
 * Returns heap-allocated updated escrow on success, NULL on failure.
 */
AetherNetTradeEscrow *aethernet_market_advance_escrow(
    const char    *escrow_id,
    AetherNetTradeState new_state,
    const char    *actor_id,
    const char    *notes
);

/**
 * Free a heap-allocated AetherNetMarketListing.
 */
void aethernet_market_free_listing(AetherNetMarketListing *listing);

/**
 * Free a heap-allocated AetherNetTradeEscrow.
 */
void aethernet_market_free_escrow(AetherNetTradeEscrow *escrow);

#ifdef __cplusplus
}
#endif

#endif /* AETHERNET_MARKET_H */
