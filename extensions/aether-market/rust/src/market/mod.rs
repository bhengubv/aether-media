use async_trait::async_trait;
use chrono::{DateTime, Utc};
use serde::{Deserialize, Serialize};
use uuid::Uuid;

// ── Enums ────────────────────────────────────────────────────────────────────

#[derive(Debug, Clone, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "SCREAMING_SNAKE_CASE")]
pub enum PoVTransport {
    Mesh,
    Bluetooth,
    Nfc,
    QrCode,
    DirectLink,
}

#[derive(Debug, Clone, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "SCREAMING_SNAKE_CASE")]
pub enum MarketCategory {
    Goods,
    Services,
    Digital,
    Food,
    Transport,
    Housing,
    Labour,
    Skills,
    Barter,
    Other,
}

#[derive(Debug, Clone, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "SCREAMING_SNAKE_CASE")]
pub enum TradeState {
    Initiated,
    Funded,
    GoodsSent,
    GoodsReceived,
    Disputed,
    Resolved,
    Cancelled,
    Expired,
    Completed,
}

#[derive(Debug, Clone, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "SCREAMING_SNAKE_CASE")]
pub enum TradeRole {
    Buyer,
    Seller,
    Arbiter,
}

// ── Structs ──────────────────────────────────────────────────────────────────

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct PoVToken {
    pub id: Uuid,
    pub issuer_id: Uuid,
    pub subject_id: Uuid,
    pub context: String,
    pub claim: String,
    pub evidence: String,
    pub transport: PoVTransport,
    pub signature: String,
    pub public_key_hint: String,
    pub weight: f64,
    pub is_revoked: bool,
    pub revoked_reason: String,
    pub issued_at: DateTime<Utc>,
    pub expires_at: Option<DateTime<Utc>>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct PoVScore {
    pub subject_id: Uuid,
    pub overall_score: f64,
    pub trade_score: f64,
    pub reliability_score: f64,
    pub response_score: f64,
    pub dispute_score: f64,
    pub token_count: i32,
    pub positive_tokens: i32,
    pub negative_tokens: i32,
    pub neutral_tokens: i32,
    pub successful_trades: i32,
    pub failed_trades: i32,
    pub disputes_raised: i32,
    pub disputes_resolved: i32,
    pub level: String,
    pub last_updated: DateTime<Utc>,
}

impl PoVScore {
    pub fn trust_percent(&self) -> f64 {
        self.overall_score.clamp(0.0, 100.0)
    }

    pub fn completion_rate(&self) -> f64 {
        let total = self.successful_trades + self.failed_trades;
        if total == 0 {
            return 0.0;
        }
        (self.successful_trades as f64 / total as f64) * 100.0
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct MarketListing {
    pub id: Uuid,
    pub seller_id: Uuid,
    pub space_id: Option<Uuid>,
    pub geo_hash: String,
    pub category: MarketCategory,
    pub title: String,
    pub description: String,
    pub price_amount: f64,
    pub price_currency: String,
    pub accepts_barter: bool,
    pub barter_description: String,
    pub image_urls: Vec<String>,
    pub tags: Vec<String>,
    pub is_available: bool,
    pub quantity: i32,
    pub requires_escrow: bool,
    pub minimum_pov_score: f64,
    pub view_count: i32,
    pub enquiry_count: i32,
    pub created_at: DateTime<Utc>,
    pub updated_at: DateTime<Utc>,
    pub expires_at: Option<DateTime<Utc>>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct TradeEscrow {
    pub id: Uuid,
    pub listing_id: Uuid,
    pub buyer_id: Uuid,
    pub seller_id: Uuid,
    pub arbiter_id: Option<Uuid>,
    pub state: TradeState,
    pub amount: f64,
    pub currency: String,
    pub description: String,
    pub buyer_pov_score: f64,
    pub seller_pov_score: f64,
    pub buyer_confirmed: bool,
    pub seller_confirmed: bool,
    pub dispute_reason: String,
    pub resolution_notes: String,
    pub escrow_address: String,
    pub mesh_transaction_id: String,
    pub timeout_hours: i32,
    pub created_at: DateTime<Utc>,
    pub updated_at: DateTime<Utc>,
    pub completed_at: Option<DateTime<Utc>>,
    pub expires_at: Option<DateTime<Utc>>,
}

impl TradeEscrow {
    pub fn is_active(&self) -> bool {
        matches!(
            self.state,
            TradeState::Initiated
                | TradeState::Funded
                | TradeState::GoodsSent
                | TradeState::Disputed
        )
    }

    pub fn is_terminal(&self) -> bool {
        matches!(
            self.state,
            TradeState::Completed
                | TradeState::Cancelled
                | TradeState::Expired
                | TradeState::Resolved
        )
    }
}

// ── Traits ───────────────────────────────────────────────────────────────────

#[async_trait]
pub trait PoVService: Send + Sync {
    async fn issue_token(
        &self,
        token: PoVToken,
    ) -> Result<PoVToken, Box<dyn std::error::Error + Send + Sync>>;

    async fn revoke_token(
        &self,
        token_id: Uuid,
        reason: &str,
    ) -> Result<bool, Box<dyn std::error::Error + Send + Sync>>;

    async fn get_score(
        &self,
        subject_id: Uuid,
    ) -> Result<PoVScore, Box<dyn std::error::Error + Send + Sync>>;

    async fn get_tokens_for(
        &self,
        subject_id: Uuid,
    ) -> Result<Vec<PoVToken>, Box<dyn std::error::Error + Send + Sync>>;

    async fn get_tokens_by(
        &self,
        issuer_id: Uuid,
    ) -> Result<Vec<PoVToken>, Box<dyn std::error::Error + Send + Sync>>;

    async fn verify_token(
        &self,
        token_id: Uuid,
    ) -> Result<bool, Box<dyn std::error::Error + Send + Sync>>;

    async fn sync_tokens(
        &self,
        peer_node_id: &str,
    ) -> Result<usize, Box<dyn std::error::Error + Send + Sync>>;
}

#[async_trait]
pub trait MarketService: Send + Sync {
    async fn create_listing(
        &self,
        listing: MarketListing,
    ) -> Result<MarketListing, Box<dyn std::error::Error + Send + Sync>>;

    async fn update_listing(
        &self,
        listing: MarketListing,
    ) -> Result<MarketListing, Box<dyn std::error::Error + Send + Sync>>;

    async fn delete_listing(
        &self,
        listing_id: Uuid,
        requester_id: Uuid,
    ) -> Result<bool, Box<dyn std::error::Error + Send + Sync>>;

    async fn get_listing(
        &self,
        listing_id: Uuid,
    ) -> Result<Option<MarketListing>, Box<dyn std::error::Error + Send + Sync>>;

    async fn search(
        &self,
        query: &str,
        category: Option<MarketCategory>,
        geo_hash: Option<&str>,
    ) -> Result<Vec<MarketListing>, Box<dyn std::error::Error + Send + Sync>>;

    async fn list_by_seller(
        &self,
        seller_id: Uuid,
        limit: usize,
        offset: usize,
    ) -> Result<Vec<MarketListing>, Box<dyn std::error::Error + Send + Sync>>;

    async fn list_by_space(
        &self,
        space_id: Uuid,
        limit: usize,
        offset: usize,
    ) -> Result<Vec<MarketListing>, Box<dyn std::error::Error + Send + Sync>>;

    async fn initiate_escrow(
        &self,
        listing_id: Uuid,
        buyer_id: Uuid,
    ) -> Result<TradeEscrow, Box<dyn std::error::Error + Send + Sync>>;

    async fn fund_escrow(
        &self,
        escrow_id: Uuid,
        buyer_id: Uuid,
    ) -> Result<TradeEscrow, Box<dyn std::error::Error + Send + Sync>>;

    async fn confirm_delivery(
        &self,
        escrow_id: Uuid,
        buyer_id: Uuid,
    ) -> Result<TradeEscrow, Box<dyn std::error::Error + Send + Sync>>;

    async fn confirm_dispatch(
        &self,
        escrow_id: Uuid,
        seller_id: Uuid,
    ) -> Result<TradeEscrow, Box<dyn std::error::Error + Send + Sync>>;

    async fn raise_dispute(
        &self,
        escrow_id: Uuid,
        raiser_id: Uuid,
        reason: &str,
    ) -> Result<TradeEscrow, Box<dyn std::error::Error + Send + Sync>>;

    async fn resolve_dispute(
        &self,
        escrow_id: Uuid,
        arbiter_id: Uuid,
        notes: &str,
        favour_buyer: bool,
    ) -> Result<TradeEscrow, Box<dyn std::error::Error + Send + Sync>>;

    async fn cancel_escrow(
        &self,
        escrow_id: Uuid,
        requester_id: Uuid,
    ) -> Result<TradeEscrow, Box<dyn std::error::Error + Send + Sync>>;

    async fn get_escrow(
        &self,
        escrow_id: Uuid,
    ) -> Result<Option<TradeEscrow>, Box<dyn std::error::Error + Send + Sync>>;
}
