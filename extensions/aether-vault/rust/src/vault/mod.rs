use async_trait::async_trait;
use chrono::{DateTime, Utc};
use serde::{Deserialize, Serialize};
use uuid::Uuid;

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct VaultManifest {
    pub id: Uuid,
    pub owner_id: Uuid,
    pub name: String,
    pub description: String,
    pub original_size_bytes: i64,
    pub encoded_size_bytes: i64,
    pub shard_count: i32,
    pub parity_shard_count: i32,
    pub min_shards_for_recovery: i32,
    pub checksum: String,
    pub checksum_algorithm: String,
    pub encryption_algorithm: String,
    pub encrypted_key_hint: String,
    pub content_type: String,
    pub tags: Vec<String>,
    pub shard_ids: Vec<Uuid>,
    pub replication_factor: i32,
    pub created_at: DateTime<Utc>,
    pub updated_at: DateTime<Utc>,
    pub expires_at: Option<DateTime<Utc>>,
    pub metadata: std::collections::HashMap<String, String>,
}

impl VaultManifest {
    pub fn new(owner_id: Uuid, name: impl Into<String>, checksum: impl Into<String>) -> Self {
        let now = Utc::now();
        Self {
            id: Uuid::new_v4(),
            owner_id,
            name: name.into(),
            description: String::new(),
            original_size_bytes: 0,
            encoded_size_bytes: 0,
            shard_count: 0,
            parity_shard_count: 0,
            min_shards_for_recovery: 0,
            checksum: checksum.into(),
            checksum_algorithm: "sha256".into(),
            encryption_algorithm: "AES-256-GCM".into(),
            encrypted_key_hint: String::new(),
            content_type: "application/octet-stream".into(),
            tags: Vec::new(),
            shard_ids: Vec::new(),
            replication_factor: 3,
            created_at: now,
            updated_at: now,
            expires_at: None,
            metadata: std::collections::HashMap::new(),
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct VaultShard {
    pub id: Uuid,
    pub manifest_id: Uuid,
    pub shard_index: i32,
    pub is_parity: bool,
    pub size_bytes: i64,
    pub checksum: String,
    pub checksum_algorithm: String,
    pub node_id: String,
    pub node_address: String,
    pub storage_key: String,
    pub is_available: bool,
    pub last_verified_at: Option<DateTime<Utc>>,
    pub created_at: DateTime<Utc>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct VaultHealth {
    pub manifest_id: Uuid,
    pub total_shards: i32,
    pub available_shards: i32,
    pub parity_shards: i32,
    pub available_parity_shards: i32,
    pub min_shards_for_recovery: i32,
    pub replication_factor: i32,
    pub degraded_nodes: Vec<String>,
    pub last_checked_at: DateTime<Utc>,
}

impl VaultHealth {
    pub fn is_recoverable(&self) -> bool {
        self.available_shards >= self.min_shards_for_recovery
    }

    pub fn is_healthy(&self) -> bool {
        self.available_shards == self.total_shards
            && self.available_parity_shards == self.parity_shards
    }

    pub fn is_degraded(&self) -> bool {
        self.is_recoverable() && !self.is_healthy()
    }

    pub fn health_percent(&self) -> f64 {
        if self.total_shards == 0 {
            return 0.0;
        }
        (self.available_shards as f64 / self.total_shards as f64) * 100.0
    }
}

#[async_trait]
pub trait VaultService: Send + Sync {
    async fn store(
        &self,
        owner_id: Uuid,
        name: &str,
        data: Vec<u8>,
        tags: Vec<String>,
    ) -> Result<VaultManifest, Box<dyn std::error::Error + Send + Sync>>;

    async fn recover(
        &self,
        manifest_id: Uuid,
        requester_id: Uuid,
    ) -> Result<Vec<u8>, Box<dyn std::error::Error + Send + Sync>>;

    async fn health(
        &self,
        manifest_id: Uuid,
    ) -> Result<VaultHealth, Box<dyn std::error::Error + Send + Sync>>;

    async fn delete(
        &self,
        manifest_id: Uuid,
        requester_id: Uuid,
    ) -> Result<bool, Box<dyn std::error::Error + Send + Sync>>;

    async fn list_manifests(
        &self,
        owner_id: Uuid,
        limit: usize,
        offset: usize,
    ) -> Result<Vec<VaultManifest>, Box<dyn std::error::Error + Send + Sync>>;

    async fn replicate_shard(
        &self,
        shard_id: Uuid,
        target_node_id: &str,
    ) -> Result<VaultShard, Box<dyn std::error::Error + Send + Sync>>;

    async fn verify_shard(
        &self,
        shard_id: Uuid,
    ) -> Result<bool, Box<dyn std::error::Error + Send + Sync>>;

    async fn get_manifest(
        &self,
        manifest_id: Uuid,
    ) -> Result<Option<VaultManifest>, Box<dyn std::error::Error + Send + Sync>>;

    async fn get_shard(
        &self,
        shard_id: Uuid,
    ) -> Result<Option<VaultShard>, Box<dyn std::error::Error + Send + Sync>>;
}
