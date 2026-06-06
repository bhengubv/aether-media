from __future__ import annotations

from abc import ABC, abstractmethod
from uuid import UUID

from .models import MarketCategory, MarketListing, PoVScore, PoVToken, TradeEscrow


class PoVService(ABC):
    """Abstract base class for Proof-of-Value trust management."""

    @abstractmethod
    async def issue_token(self, token: PoVToken) -> PoVToken:
        """Sign and publish a PoV token to the mesh."""
        ...

    @abstractmethod
    async def revoke_token(self, token_id: UUID, reason: str) -> bool:
        """Revoke a token by ID. Returns True if found."""
        ...

    @abstractmethod
    async def get_score(self, subject_id: UUID) -> PoVScore:
        """Compute the aggregated trust score for a user."""
        ...

    @abstractmethod
    async def get_tokens_for(self, subject_id: UUID) -> list[PoVToken]:
        """Return all tokens where subject_id is the target."""
        ...

    @abstractmethod
    async def get_tokens_by(self, issuer_id: UUID) -> list[PoVToken]:
        """Return all tokens issued by issuer_id."""
        ...

    @abstractmethod
    async def verify_token(self, token_id: UUID) -> bool:
        """Cryptographically verify a token's signature."""
        ...

    @abstractmethod
    async def sync_tokens(self, peer_node_id: str) -> int:
        """Sync token set with a remote peer. Returns count exchanged."""
        ...


class PoVServiceImpl(PoVService):
    """Stub implementation."""

    async def issue_token(self, token: PoVToken) -> PoVToken:
        raise NotImplementedError("not implemented")

    async def revoke_token(self, token_id: UUID, reason: str) -> bool:
        raise NotImplementedError("not implemented")

    async def get_score(self, subject_id: UUID) -> PoVScore:
        raise NotImplementedError("not implemented")

    async def get_tokens_for(self, subject_id: UUID) -> list[PoVToken]:
        raise NotImplementedError("not implemented")

    async def get_tokens_by(self, issuer_id: UUID) -> list[PoVToken]:
        raise NotImplementedError("not implemented")

    async def verify_token(self, token_id: UUID) -> bool:
        raise NotImplementedError("not implemented")

    async def sync_tokens(self, peer_node_id: str) -> int:
        raise NotImplementedError("not implemented")


class MarketService(ABC):
    """Abstract base class for offline-capable mesh commerce."""

    @abstractmethod
    async def create_listing(self, listing: MarketListing) -> MarketListing:
        ...

    @abstractmethod
    async def update_listing(self, listing: MarketListing) -> MarketListing:
        ...

    @abstractmethod
    async def delete_listing(self, listing_id: UUID, requester_id: UUID) -> bool:
        ...

    @abstractmethod
    async def get_listing(self, listing_id: UUID) -> MarketListing | None:
        ...

    @abstractmethod
    async def search(
        self,
        query: str,
        category: MarketCategory | None = None,
        geo_hash: str | None = None,
    ) -> list[MarketListing]:
        ...

    @abstractmethod
    async def list_by_seller(
        self, seller_id: UUID, limit: int = 50, offset: int = 0
    ) -> list[MarketListing]:
        ...

    @abstractmethod
    async def list_by_space(
        self, space_id: UUID, limit: int = 50, offset: int = 0
    ) -> list[MarketListing]:
        ...

    @abstractmethod
    async def initiate_escrow(self, listing_id: UUID, buyer_id: UUID) -> TradeEscrow:
        ...

    @abstractmethod
    async def fund_escrow(self, escrow_id: UUID, buyer_id: UUID) -> TradeEscrow:
        ...

    @abstractmethod
    async def confirm_delivery(self, escrow_id: UUID, buyer_id: UUID) -> TradeEscrow:
        ...

    @abstractmethod
    async def confirm_dispatch(self, escrow_id: UUID, seller_id: UUID) -> TradeEscrow:
        ...

    @abstractmethod
    async def raise_dispute(
        self, escrow_id: UUID, raiser_id: UUID, reason: str
    ) -> TradeEscrow:
        ...

    @abstractmethod
    async def resolve_dispute(
        self,
        escrow_id: UUID,
        arbiter_id: UUID,
        notes: str,
        favour_buyer: bool,
    ) -> TradeEscrow:
        ...

    @abstractmethod
    async def cancel_escrow(self, escrow_id: UUID, requester_id: UUID) -> TradeEscrow:
        ...

    @abstractmethod
    async def get_escrow(self, escrow_id: UUID) -> TradeEscrow | None:
        ...


class MarketServiceImpl(MarketService):
    """Stub implementation."""

    async def create_listing(self, listing: MarketListing) -> MarketListing:
        raise NotImplementedError("not implemented")

    async def update_listing(self, listing: MarketListing) -> MarketListing:
        raise NotImplementedError("not implemented")

    async def delete_listing(self, listing_id: UUID, requester_id: UUID) -> bool:
        raise NotImplementedError("not implemented")

    async def get_listing(self, listing_id: UUID) -> MarketListing | None:
        raise NotImplementedError("not implemented")

    async def search(self, query: str, category: MarketCategory | None = None, geo_hash: str | None = None) -> list[MarketListing]:
        raise NotImplementedError("not implemented")

    async def list_by_seller(self, seller_id: UUID, limit: int = 50, offset: int = 0) -> list[MarketListing]:
        raise NotImplementedError("not implemented")

    async def list_by_space(self, space_id: UUID, limit: int = 50, offset: int = 0) -> list[MarketListing]:
        raise NotImplementedError("not implemented")

    async def initiate_escrow(self, listing_id: UUID, buyer_id: UUID) -> TradeEscrow:
        raise NotImplementedError("not implemented")

    async def fund_escrow(self, escrow_id: UUID, buyer_id: UUID) -> TradeEscrow:
        raise NotImplementedError("not implemented")

    async def confirm_delivery(self, escrow_id: UUID, buyer_id: UUID) -> TradeEscrow:
        raise NotImplementedError("not implemented")

    async def confirm_dispatch(self, escrow_id: UUID, seller_id: UUID) -> TradeEscrow:
        raise NotImplementedError("not implemented")

    async def raise_dispute(self, escrow_id: UUID, raiser_id: UUID, reason: str) -> TradeEscrow:
        raise NotImplementedError("not implemented")

    async def resolve_dispute(self, escrow_id: UUID, arbiter_id: UUID, notes: str, favour_buyer: bool) -> TradeEscrow:
        raise NotImplementedError("not implemented")

    async def cancel_escrow(self, escrow_id: UUID, requester_id: UUID) -> TradeEscrow:
        raise NotImplementedError("not implemented")

    async def get_escrow(self, escrow_id: UUID) -> TradeEscrow | None:
        raise NotImplementedError("not implemented")
