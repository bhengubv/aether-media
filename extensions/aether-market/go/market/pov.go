// SPDX-License-Identifier: MIT
package market

import "time"

// PoVTransport enumerates the short-range transport protocols valid for
// Proof-of-Vicinity attestation.
type PoVTransport int

const (
	// PoVTransportBLE is Bluetooth Low Energy (~10 m range).
	PoVTransportBLE PoVTransport = 0
	// PoVTransportNFC is Near Field Communication (~4 cm range).
	PoVTransportNFC PoVTransport = 1
	// PoVTransportNearLink is Huawei NearLink / SparkLink (~10 m range).
	PoVTransportNearLink PoVTransport = 2
)

// PoVToken is a cryptographically signed token proving that two devices were
// physically co-located via a short-range transport at a specific moment.
// Both parties sign the same canonical payload so neither can forge the event.
type PoVToken struct {
	// WitnessUhid is the universal host ID of the witnessing device.
	WitnessUhid string `json:"witness_uhid"`
	// SubjectUhid is the universal host ID of the subject device.
	SubjectUhid string `json:"subject_uhid"`
	// TimestampUtc is the UTC timestamp at which the proximity event occurred.
	TimestampUtc time.Time `json:"timestamp_utc"`
	// TransportUsed is the short-range transport protocol used for the handshake.
	TransportUsed PoVTransport `json:"transport_used"`
	// WitnessSignature is the Ed25519 signature produced by the witness.
	WitnessSignature []byte `json:"witness_signature"`
	// SubjectSignature is the Ed25519 signature produced by the subject.
	SubjectSignature []byte `json:"subject_signature"`
}

// PoVScore is the aggregated Proof-of-Vicinity reputation score for a mesh
// node, derived from witnessed co-location events with a 6-month half-life
// decay applied at query time.
type PoVScore struct {
	// Uhid is the universal host ID of the node this score applies to.
	Uhid string `json:"uhid"`
	// UniqueWitnesses is the number of distinct UHIDs that witnessed proximity.
	UniqueWitnesses int `json:"unique_witnesses"`
	// WeightedScore is the decay-adjusted composite score.
	WeightedScore float64 `json:"weighted_score"`
	// LastUpdated is the UTC timestamp of the most recent PoV token factored in.
	LastUpdated time.Time `json:"last_updated"`
}
