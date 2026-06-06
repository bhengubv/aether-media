from __future__ import annotations

from dataclasses import dataclass, field
from datetime import datetime
from enum import Enum
from typing import Optional
from uuid import UUID, uuid4


class PoVTransport(str, Enum):
    MESH = "MESH"
    BLUETOOTH = "BLUETOOTH"
    NFC = "NFC"
    QR_CODE = "QR_CODE"
    DIRECT_LINK = "DIRECT_LINK"


class MarketCategory(str, Enum):
    GOODS = "GOODS"
    SERVICES = "SERVICES"
    DIGITAL = "DIGITAL"
    FOOD = "FOOD"
    TRANSPORT = "TRANSPORT"
    HOUSING = "HOUSING"
    LABOUR = "LABOUR"
    SKILLS = "SKILLS"
    BARTER = "BARTER"
    OTHER = "OTHER"


class TradeState(str, Enum):
    INITIATED = "INITIATED"
    FUNDED = "FUNDED"
    GOODS_SENT = "GOODS_SENT"
    GOODS_RECEIVED = "GOODS_RECEIVED"
    DISPUTED = "DISPUTED"
    RESOLVED = "RESOLVED"
    CANCELLED = "CANCELLED"
    EXPIRED = "EXPIRED"
    COMPLETED = "COMPLETED"


class TradeRole(str, Enum):
    BUYER = "BUYER"
    SELLER = "SELLER"
    ARBITER = "ARBITER"


@dataclass
class PoVToken:
    issuer_id: UUID
    subject_id: UUID
    context: str
    claim: str
    signature: str
    id: UUID = field(default_factory=uuid4)
    evidence: str = ""
    transport: PoVTransport = PoVTransport.MESH
    public_key_hint: str = ""
    weight: float = 1.0
    is_revoked: bool = False
    revoked_reason: str = ""
    issued_at: datetime = field(default_factory=datetime.utcnow)
    expires_at: Optional[datetime] = None


@dataclass
class PoVScore:
    subject_id: UUID
    overall_score: float
    trade_score: float = 0.0
    reliability_score: float = 0.0
    response_score: float = 0.0
    dispute_score: float = 0.0
    token_count: int = 0
    positive_tokens: int = 0
    negative_tokens: int = 0
    neutral_tokens: int = 0
    successful_trades: int = 0
    failed_trades: int = 0
    disputes_raised: int = 0
    disputes_resolved: int = 0
    level: str = "UNRANKED"
    last_updated: datetime = field(default_factory=datetime.utcnow)

    @property
    def trust_percent(self) -> float:
        return max(0.0, min(100.0, self.overall_score))

    @property
    def completion_rate(self) -> float:
        total = self.successful_trades + self.failed_trades
        if total == 0:
            return 0.0
        return (self.successful_trades / total) * 100.0


@dataclass
class MarketListing:
    seller_id: UUID
    category: MarketCategory
    title: str
    description: str
    price_amount: float
    id: UUID = field(default_factory=uuid4)
    space_id: Optional[UUID] = None
    geo_hash: str = ""
    price_currency: str = "ZAR"
    accepts_barter: bool = False
    barter_description: str = ""
    image_urls: list[str] = field(default_factory=list)
    tags: list[str] = field(default_factory=list)
    is_available: bool = True
    quantity: int = 1
    requires_escrow: bool = False
    minimum_pov_score: float = 0.0
    view_count: int = 0
    enquiry_count: int = 0
    created_at: datetime = field(default_factory=datetime.utcnow)
    updated_at: datetime = field(default_factory=datetime.utcnow)
    expires_at: Optional[datetime] = None


@dataclass
class TradeEscrow:
    listing_id: UUID
    buyer_id: UUID
    seller_id: UUID
    amount: float
    id: UUID = field(default_factory=uuid4)
    arbiter_id: Optional[UUID] = None
    state: TradeState = TradeState.INITIATED
    currency: str = "ZAR"
    description: str = ""
    buyer_pov_score: float = 0.0
    seller_pov_score: float = 0.0
    buyer_confirmed: bool = False
    seller_confirmed: bool = False
    dispute_reason: str = ""
    resolution_notes: str = ""
    escrow_address: str = ""
    mesh_transaction_id: str = ""
    timeout_hours: int = 72
    created_at: datetime = field(default_factory=datetime.utcnow)
    updated_at: datetime = field(default_factory=datetime.utcnow)
    completed_at: Optional[datetime] = None
    expires_at: Optional[datetime] = None

    _ACTIVE_STATES = {
        TradeState.INITIATED,
        TradeState.FUNDED,
        TradeState.GOODS_SENT,
        TradeState.DISPUTED,
    }
    _TERMINAL_STATES = {
        TradeState.COMPLETED,
        TradeState.CANCELLED,
        TradeState.EXPIRED,
        TradeState.RESOLVED,
    }

    @property
    def is_active(self) -> bool:
        return self.state in self._ACTIVE_STATES

    @property
    def is_terminal(self) -> bool:
        return self.state in self._TERMINAL_STATES
