use async_trait::async_trait;
use chrono::{DateTime, Utc};
use serde::{Deserialize, Serialize};
use uuid::Uuid;

#[derive(Debug, Clone, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "SCREAMING_SNAKE_CASE")]
pub enum BreadcrumbType {
    Post,
    Event,
    Alert,
    Offer,
    Notice,
    Pinned,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct SpaceBreadcrumb {
    pub id: Uuid,
    pub space_id: Uuid,
    pub author_id: Uuid,
    pub geo_hash: String,
    #[serde(rename = "type")]
    pub breadcrumb_type: BreadcrumbType,
    pub title: String,
    pub body: String,
    pub media_urls: Vec<String>,
    pub tags: Vec<String>,
    pub expires_at: Option<DateTime<Utc>>,
    pub is_pinned: bool,
    pub reaction_count: i32,
    pub reply_count: i32,
    pub created_at: DateTime<Utc>,
    pub updated_at: DateTime<Utc>,
}

impl SpaceBreadcrumb {
    pub fn new(
        space_id: Uuid,
        author_id: Uuid,
        geo_hash: impl Into<String>,
        breadcrumb_type: BreadcrumbType,
        title: impl Into<String>,
        body: impl Into<String>,
    ) -> Self {
        let now = Utc::now();
        Self {
            id: Uuid::new_v4(),
            space_id,
            author_id,
            geo_hash: geo_hash.into(),
            breadcrumb_type,
            title: title.into(),
            body: body.into(),
            media_urls: Vec::new(),
            tags: Vec::new(),
            expires_at: None,
            is_pinned: false,
            reaction_count: 0,
            reply_count: 0,
            created_at: now,
            updated_at: now,
        }
    }
}

/// Newtype wrapper around a GeoHash string.
#[derive(Debug, Clone, PartialEq, Eq, Hash, Serialize, Deserialize)]
pub struct GeoHash(pub String);

impl GeoHash {
    const BASE32: &'static [u8; 32] = b"0123456789bcdefghjkmnpqrstuvwxyz";

    /// Encode geographic coordinates into a GeoHash string.
    ///
    /// # Arguments
    /// * `lat` - Latitude in degrees (-90..=90)
    /// * `lon` - Longitude in degrees (-180..=180)
    /// * `precision` - Number of characters (1..=12, default 6)
    pub fn from_coordinates(lat: f64, lon: f64, precision: usize) -> Self {
        assert!((-90.0..=90.0).contains(&lat), "Latitude out of range");
        assert!((-180.0..=180.0).contains(&lon), "Longitude out of range");
        assert!((1..=12).contains(&precision), "Precision out of range");

        let mut min_lat = -90.0_f64;
        let mut max_lat = 90.0_f64;
        let mut min_lon = -180.0_f64;
        let mut max_lon = 180.0_f64;

        let mut hash = String::with_capacity(precision);
        let mut is_even = true;
        let mut bit: u32 = 0;
        let mut ch: usize = 0;

        while hash.len() < precision {
            if is_even {
                let mid = (min_lon + max_lon) / 2.0;
                if lon >= mid {
                    ch |= 1 << (4 - bit);
                    min_lon = mid;
                } else {
                    max_lon = mid;
                }
            } else {
                let mid = (min_lat + max_lat) / 2.0;
                if lat >= mid {
                    ch |= 1 << (4 - bit);
                    min_lat = mid;
                } else {
                    max_lat = mid;
                }
            }
            is_even = !is_even;
            if bit < 4 {
                bit += 1;
            } else {
                hash.push(Self::BASE32[ch] as char);
                bit = 0;
                ch = 0;
            }
        }

        GeoHash(hash)
    }
}

impl std::fmt::Display for GeoHash {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(f, "{}", self.0)
    }
}

#[async_trait]
pub trait SpaceService: Send + Sync {
    async fn drop_breadcrumb(
        &self,
        breadcrumb: SpaceBreadcrumb,
    ) -> Result<SpaceBreadcrumb, Box<dyn std::error::Error + Send + Sync>>;

    async fn scan(
        &self,
        geo_hash: &str,
        radius_km: f64,
    ) -> Result<Vec<SpaceBreadcrumb>, Box<dyn std::error::Error + Send + Sync>>;

    async fn pin_breadcrumb(
        &self,
        breadcrumb_id: Uuid,
        space_id: Uuid,
    ) -> Result<SpaceBreadcrumb, Box<dyn std::error::Error + Send + Sync>>;

    async fn unpin_breadcrumb(
        &self,
        breadcrumb_id: Uuid,
        space_id: Uuid,
    ) -> Result<SpaceBreadcrumb, Box<dyn std::error::Error + Send + Sync>>;

    async fn delete_breadcrumb(
        &self,
        breadcrumb_id: Uuid,
        requester_id: Uuid,
    ) -> Result<bool, Box<dyn std::error::Error + Send + Sync>>;

    async fn get_by_id(
        &self,
        breadcrumb_id: Uuid,
    ) -> Result<Option<SpaceBreadcrumb>, Box<dyn std::error::Error + Send + Sync>>;

    async fn list_by_space(
        &self,
        space_id: Uuid,
        limit: usize,
        offset: usize,
    ) -> Result<Vec<SpaceBreadcrumb>, Box<dyn std::error::Error + Send + Sync>>;

    async fn react(
        &self,
        breadcrumb_id: Uuid,
        user_id: Uuid,
        reaction: &str,
    ) -> Result<i32, Box<dyn std::error::Error + Send + Sync>>;
}
