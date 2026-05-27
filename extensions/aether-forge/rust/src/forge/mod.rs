use async_trait::async_trait;
use chrono::{DateTime, Utc};
use serde::{Deserialize, Serialize};
use uuid::Uuid;

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct ForgeEntry {
    pub id: Uuid,
    pub package_id: String,
    pub ecosystem: String,
    pub version: String,
    pub name: String,
    pub description: String,
    pub author: String,
    pub license_id: String,
    pub size_bytes: i64,
    pub checksum: String,
    pub checksum_algorithm: String,
    pub download_url: String,
    pub mirror_urls: Vec<String>,
    pub dependencies: Vec<String>,
    pub tags: Vec<String>,
    pub is_verified: bool,
    pub download_count: i64,
    pub cached_at: DateTime<Utc>,
    pub expires_at: Option<DateTime<Utc>>,
    pub metadata: std::collections::HashMap<String, String>,
}

impl ForgeEntry {
    pub fn new(
        package_id: impl Into<String>,
        ecosystem: impl Into<String>,
        version: impl Into<String>,
        name: impl Into<String>,
        checksum: impl Into<String>,
        download_url: impl Into<String>,
    ) -> Self {
        Self {
            id: Uuid::new_v4(),
            package_id: package_id.into(),
            ecosystem: ecosystem.into(),
            version: version.into(),
            name: name.into(),
            description: String::new(),
            author: String::new(),
            license_id: String::new(),
            size_bytes: 0,
            checksum: checksum.into(),
            checksum_algorithm: "sha256".into(),
            download_url: download_url.into(),
            mirror_urls: Vec::new(),
            dependencies: Vec::new(),
            tags: Vec::new(),
            is_verified: false,
            download_count: 0,
            cached_at: Utc::now(),
            expires_at: None,
            metadata: std::collections::HashMap::new(),
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct ForgeStats {
    pub total_entries: i64,
    pub total_size_bytes: i64,
    pub total_downloads: i64,
    pub unique_ecosystems: i32,
    pub unique_packages: i64,
    pub verified_packages: i64,
    pub hit_rate: f64,
    pub miss_rate: f64,
    pub average_package_size_bytes: i64,
    pub peak_downloads_per_hour: i64,
    pub active_peers: i32,
    pub last_updated: DateTime<Utc>,
    pub ecosystem_breakdown: std::collections::HashMap<String, i64>,
}

impl Default for ForgeStats {
    fn default() -> Self {
        Self {
            total_entries: 0,
            total_size_bytes: 0,
            total_downloads: 0,
            unique_ecosystems: 0,
            unique_packages: 0,
            verified_packages: 0,
            hit_rate: 0.0,
            miss_rate: 0.0,
            average_package_size_bytes: 0,
            peak_downloads_per_hour: 0,
            active_peers: 0,
            last_updated: Utc::now(),
            ecosystem_breakdown: std::collections::HashMap::new(),
        }
    }
}

#[async_trait]
pub trait ForgeService: Send + Sync {
    async fn query(
        &self,
        package_id: &str,
        ecosystem: &str,
        version: Option<&str>,
    ) -> Result<Option<ForgeEntry>, Box<dyn std::error::Error + Send + Sync>>;

    async fn cache(
        &self,
        entry: ForgeEntry,
    ) -> Result<ForgeEntry, Box<dyn std::error::Error + Send + Sync>>;

    async fn fetch(
        &self,
        package_id: &str,
        ecosystem: &str,
        version: &str,
    ) -> Result<Vec<u8>, Box<dyn std::error::Error + Send + Sync>>;

    async fn stats(
        &self,
    ) -> Result<ForgeStats, Box<dyn std::error::Error + Send + Sync>>;

    async fn evict(
        &self,
        entry_id: Uuid,
    ) -> Result<bool, Box<dyn std::error::Error + Send + Sync>>;

    async fn list_by_ecosystem(
        &self,
        ecosystem: &str,
        limit: usize,
        offset: usize,
    ) -> Result<Vec<ForgeEntry>, Box<dyn std::error::Error + Send + Sync>>;

    async fn search(
        &self,
        query: &str,
        ecosystem: Option<&str>,
    ) -> Result<Vec<ForgeEntry>, Box<dyn std::error::Error + Send + Sync>>;

    async fn sync(
        &self,
        peer_node_id: &str,
    ) -> Result<usize, Box<dyn std::error::Error + Send + Sync>>;
}
