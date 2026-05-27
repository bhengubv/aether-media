# eKYC Pathway via Proof-of-Vicinity

## Summary

10+ unique Proof-of-Vicinity (PoV) witnesses satisfy the simplified due-diligence
requirements of **SARB Exemption 17** (Directive 1 of 2017), enabling mobile-money
account onboarding without a phone number or government-issued ID document.

This opens a regulatory pathway for SDPKT wallet activation and low-value
account creation for the unbanked — entirely offline, entirely peer-verified.

---

## Regulatory Basis

**Directive 1 of 2017** — *Determination of Fit and Proper Requirements for
Accountable Institutions as defined in Schedule 1 of the Financial Intelligence
Centre Act, 2001 (Act No. 38 of 2001)*, issued by the South African Reserve Bank.

**Exemption 17 (simplified due diligence)** permits reduced KYC requirements for
accounts that:
- Hold a maximum balance of **R25 000** at any time
- Have a maximum monthly transaction value of **R25 000**
- Are not used for business purposes

For these accounts, the full FICA verification chain (ID document + proof of
address + source-of-funds declaration) may be replaced with a simplified process
that establishes the customer's identity to a reasonable degree.

---

## How PoV Satisfies Simplified Due Diligence

| FICA Requirement | Traditional Method | PoV Equivalent |
|---|---|---|
| Identity verification | SA ID / passport scan | 10+ community members physically vouch for the person over BLE/NFC |
| Liveness check | Manual + video selfie | BLE co-presence requires physical proximity; countersignature requires the subject's consent |
| Address / community anchor | Proof-of-address document | GeoHash cluster of PoV tokens establishes community anchor |
| Anti-Sybil protection | Phone number + telco records | Trust decay + voucher-penalisation makes mass fake-vouching economically irrational |

### Why 10 Witnesses?

10 unique witnesses is a conservative threshold chosen to match the informal-community
vouching model used in stokvels and burial societies — institutions SARB already
recognises as valid community trust networks.

- 3 witnesses → weak (family/close friends only, easy to game)
- 10 witnesses → strong (multiple independent community members; comparable to two-reference bank opening)
- 20+ witnesses → full (equivalent to telco-grade verification for Tier 2 limits)

---

## PoV Token Cryptographic Guarantees

Each `PoVToken` provides:

1. **Mutual consent** — both the witness and the subject must sign the token
   (Ed25519). Neither can issue a unilateral voucher.
2. **Transport constraint** — only BLE, NFC, or NearLink transports are accepted.
   These are short-range (≤100m), making remote forgery impossible.
3. **Temporal binding** — timestamp signed into the token prevents replay attacks.
4. **Decay** — each token's weight decays with a 6-month half-life, preventing
   "historical vouching" for someone who has since become fraudulent.
5. **Penalisation** — if a vouched-for peer triggers a verified fraud report,
   the voucher's PoVScore decreases by 20%. This makes walk-around PoV farming
   economically irrational.

---

## Account Tier Mapping

| PoV Score | Witnesses | Daily Limit | Monthly Limit | SARB Basis |
|---|---|---|---|---|
| Tier 0 | 0 | R500 | R2 500 | Exemption 17 minimum |
| Tier 1 | 3–9 | R2 000 | R10 000 | Exemption 17 community anchor |
| Tier 2 | 10–19 | R5 000 | R25 000 | Exemption 17 maximum |
| Tier 3 | 20+ | R10 000 | R50 000 | Requires supplemental ID (FIC Act standard) |

Tier 3 exceeds the Exemption 17 cap and requires a supplementary traditional
KYC step (ID document upload or biometric check via a registered FICA accountable
institution partner).

---

## Integration with SDPKT Wallet

The `PoVScore.WeightedScore` is exposed via `IPoVService.GetScoreAsync()`.
At wallet onboarding, SDPKT reads the score and maps it to the account tier above.

```csharp
var score = await povService.GetScoreAsync(userUhid);
var tier  = score.UniqueWitnesses switch {
    >= 20 => AccountTier.Tier3,
    >= 10 => AccountTier.Tier2,
    >=  3 => AccountTier.Tier1,
    _     => AccountTier.Tier0
};
```

Tier upgrades are triggered automatically when a new PoV token pushes the witness
count past the next threshold. No manual review is required for Tier 0→1 and
Tier 1→2 transitions.

---

## Compliance Notes

- Aether Media is **not** a registered FICA accountable institution. PoV scores
  are an identity-confidence signal, not a legal KYC certification.
- SDPKT's Tier 2 activation requires integration with a licensed FICA partner
  (e.g. a registered payment service provider or bank) who accepts the PoV score
  as part of their simplified due-diligence workflow under Exemption 17.
- Full legal review by a South African financial-services compliance attorney is
  required before any live deployment of Tier 1+ wallet limits.
- Global deployments must map to local equivalents (e.g. RBI India, BSP Philippines,
  BCEAO West Africa) — the PoV mechanism is jurisdiction-agnostic; the tier mapping
  is jurisdiction-specific.

---

## Open Questions

1. **FICA partner selection** — Which licensed SA PSP or bank will accept PoV as
   simplified due diligence? (Capitec, TymeBank, and African Rainbow Capital
   Financial Services are candidates given their unbanked-market positioning.)
2. **Dispute resolution** — When a fraud report is filed, who adjudicates? Purely
   mesh-gossipped reports are gameable. A hybrid approach (mesh + SDPKT admin review)
   is preferred.
3. **Biometric supplement for Tier 3** — For accounts above the Exemption 17 cap,
   what is the minimum-friction biometric flow? Options: liveness selfie + ID OCR,
   or integration with SASSA biometric database (requires DHA/SASSA MOU).
