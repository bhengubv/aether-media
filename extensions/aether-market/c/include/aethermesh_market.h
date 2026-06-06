#ifndef AETHERMESH_MARKET_H
#define AETHERMESH_MARKET_H

#include <stdint.h>
#include <stddef.h>
#include <time.h>

#ifdef __cplusplus
extern "C" {
#endif

/* ── Enums ─────────────────────────────────────────────────────────────────── */

typedef enum AetherMeshPoVTransport {
    AETHERMESH_POV_TRANSPORT_MESH        = 0,
    AETHERMESH_POV_TRANSPORT_BLUETOOTH   = 1,
    AETHERMESH_POV_TRANSPORT_NFC         = 2,
    AETHERMESH_POV_TRANSPORT_QR_CODE     = 3,
    AETHERMESH_POV_TRANSPORT_DIRECT_LINK = 4
} AetherMeshPoVTransport;

typedef enum AetherMeshMarketCategory {
    AETHERMESH_MARKET_GOODS     = 0,
    AETHERMESH_MARKET_SERVICES  = 1,
    AETHERMESH_MARKET_DIGITAL   = 2,
    AETHERMESH_MARKET_FOOD      = 3,
    AETHERMESH_MARKET_TRANSPORT = 4,
    AETHERMESH_MARKET_HOUSING   = 5,
    AETHERMESH_MARKET_LABOUR    = 6,
    AETHERMESH_MARKET_SKILLS    = 7,
    AETHERMESH_MARKET_BARTER    = 8,
    AETHERMESH_MARKET_OTHER     = 9
} AetherMeshMarketCategory;

typedef enum AetherMeshTradeState {
    AETHERMESH_TRADE_INITIATED      = 0,
    AETHERMESH_TRADE_FUNDED         = 1,
    AETHERMESH_TRADE_GOODS_SENT     = 2,
    AETHERMESH_TRADE_GOODS_RECEIVED = 3,
    AETHERMESH_TRADE_DISPUTED       = 4,
    AETHERMESH_TRADE_RESOLVED       = 5,
    AETHERMESH_TRADE_CANCELLED      = 6,
    AETHERMESH_TRADE_EXPIRED        = 7,
    AETHERMESH_TRADE_COMPLETED      = 8
} AetherMeshTradeState;

typedef enum AetherMeshTradeRole {
    AETHERMESH_TRADE_ROLE_BUYER   = 0,
    AETHERMESH_TRADE_ROLE_SELLER  = 1,
    AETHERMESH_TRADE_ROLE_ARBITER = 2
} AetherMeshTradeRole;

/* ── Structs ────────────────────────────────────────────────────────────────── */

/**
 * AetherMeshPoVToken
 * Proof-of-Value attestation token issued on the mesh.
 */
typedef struct AetherMeshPoVToken {
    char              id[37];
    char              issuer_id[37];
    char              subject_id[37];
    char             *context;          /* heap-allocated */
    char             *claim;            /* heap-allocated */
    char             *evidence;         /* heap-allocated, may be NULL */
    char             *signature;        /* heap-allocated */
    AetherMeshPoVTransport transport;
    double            weight;
    int               is_revoked;
    time_t            issued_at;
    time_t            expires_at;       /* 0 = no expiry */
} AetherMeshPoVToken;

/**
 * AetherMeshPoVScore
 * Aggregated trust score for a user.
 */
typedef struct AetherMeshPoVScore {
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
} AetherMeshPoVScore;

/**
 * AetherMeshMarketListing
 * A product or service offered on the mesh marketplace.
 */
typedef struct AetherMeshMarketListing {
    char                 id[37];
    char                 seller_id[37];
    char                 space_id[37];    /* all-zero if not set */
    char                 geo_hash[13];
    AetherMeshMarketCategory category;
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
} AetherMeshMarketListing;

/**
 * AetherMeshTradeEscrow
 * Mesh-native trade escrow record.
 */
typedef struct AetherMeshTradeEscrow {
    char            id[37];
    char            listing_id[37];
    char            buyer_id[37];
    char            seller_id[37];
    char            arbiter_id[37];    /* all-zero if not set */
    AetherMeshTradeState state;
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
} AetherMeshTradeEscrow;

/* ── PoV Function Prototypes ────────────────────────────────────────────────── */

/**
 * Issue a PoV token on the mesh.
 * Returns a heap-allocated token on success, NULL on failure.
 * Caller frees with aethermesh_pov_free_token().
 */
AetherMeshPoVToken *aethermesh_pov_issue(const AetherMeshPoVToken *token);

/**
 * Revoke a PoV token by ID.
 * Returns 1 on success, 0 otherwise.
 */
int aethermesh_pov_revoke(const char *token_id, const char *reason);

/**
 * Get the aggregated trust score for a user.
 * Returns a heap-allocated score; caller frees with aethermesh_pov_free_score().
 */
AetherMeshPoVScore *aethermesh_pov_get_score(const char *subject_id);

/**
 * Verify a token's cryptographic signature.
 * Returns 1 if valid, 0 otherwise.
 */
int aethermesh_pov_verify(const char *token_id);

/**
 * Free a heap-allocated AetherMeshPoVToken.
 */
void aethermesh_pov_free_token(AetherMeshPoVToken *token);

/**
 * Free a heap-allocated AetherMeshPoVScore.
 */
void aethermesh_pov_free_score(AetherMeshPoVScore *score);

/* ── Market Function Prototypes ─────────────────────────────────────────────── */

/**
 * Publish a market listing to the mesh.
 * Returns a heap-allocated listing on success, NULL on failure.
 * Caller frees with aethermesh_market_free_listing().
 */
AetherMeshMarketListing *aethermesh_market_create_listing(const AetherMeshMarketListing *listing);

/**
 * Retrieve a listing by ID.
 * Returns heap-allocated listing or NULL if not found.
 */
AetherMeshMarketListing *aethermesh_market_get_listing(const char *listing_id);

/**
 * Delete a listing.
 * Returns 1 on success, 0 otherwise.
 */
int aethermesh_market_delete_listing(const char *listing_id, const char *requester_id);

/**
 * Initiate an escrow for a trade.
 * Returns heap-allocated escrow on success, NULL on failure.
 * Caller frees with aethermesh_market_free_escrow().
 */
AetherMeshTradeEscrow *aethermesh_market_initiate_escrow(
    const char *listing_id,
    const char *buyer_id
);

/**
 * Advance an escrow to a new state.
 * Returns heap-allocated updated escrow on success, NULL on failure.
 */
AetherMeshTradeEscrow *aethermesh_market_advance_escrow(
    const char    *escrow_id,
    AetherMeshTradeState new_state,
    const char    *actor_id,
    const char    *notes
);

/**
 * Free a heap-allocated AetherMeshMarketListing.
 */
void aethermesh_market_free_listing(AetherMeshMarketListing *listing);

/**
 * Free a heap-allocated AetherMeshTradeEscrow.
 */
void aethermesh_market_free_escrow(AetherMeshTradeEscrow *escrow);

#ifdef __cplusplus
}
#endif

#endif /* AETHERMESH_MARKET_H */
