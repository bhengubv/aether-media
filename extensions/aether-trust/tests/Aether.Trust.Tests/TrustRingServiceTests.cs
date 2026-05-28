// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using Aether.Trust.Core;
using Aether.Trust.Verification;
using Xunit;

namespace Aether.Trust.Tests;

/// <summary>
/// Tests for <see cref="TrustRingService"/>.
///
/// Key material uses ECDSA P-256 (guaranteed on all .NET platforms) with
/// IEEE P1363 signatures — the same code path the service uses for Ed25519.
/// </summary>
public sealed class TrustRingServiceTests : IDisposable
{
    private readonly TrustRingService _sut = new();

    private const string PublisherUhid = "TEST-PUBLISHER-UHID";

    // ── Hash verification ────────────────────────────────────────────────────

    [Fact]
    public async Task VerifyAsync_CorrectHash_EmptyRing_ReturnsNoRingRequired()
    {
        var data = "hello aether"u8.ToArray();
        var hash = ComputeSha256Hex(data);

        var result = await _sut.VerifyAsync(hash, data, PublisherUhid, ContentCategory.MediaContent);

        Assert.Equal(ContentTrustStatus.NoRingRequired, result.Status);
        Assert.True(result.IsClean);
    }

    [Fact]
    public async Task VerifyAsync_WrongHash_ReturnsHashMismatch_AndQuarantines()
    {
        var data     = "real content"u8.ToArray();
        var wrongHash = new string('a', 64); // 64 hex chars, all 'a'

        var result = await _sut.VerifyAsync(wrongHash, data, PublisherUhid, ContentCategory.AppBinary);

        Assert.Equal(ContentTrustStatus.HashMismatch, result.Status);
        Assert.False(result.IsClean);
        Assert.NotNull(result.FailureReason);
        Assert.Equal(ContentTrustStatus.Quarantined, _sut.GetStatus(wrongHash));
    }

    [Fact]
    public async Task VerifyAsync_WrongHash_FiresViolationDetected()
    {
        var data      = "payload"u8.ToArray();
        var wrongHash = new string('b', 64);
        TrustViolationEvent? fired = null;
        _sut.ViolationDetected += (_, e) => fired = e;

        await _sut.VerifyAsync(wrongHash, data, PublisherUhid, ContentCategory.Document);

        Assert.NotNull(fired);
        Assert.Equal(wrongHash, fired.ContentHash);
        Assert.Equal(PublisherUhid, fired.PublisherUhid);
        Assert.Equal(ContentTrustStatus.HashMismatch, fired.FailureStatus);
    }

    // ── Ring-empty (no signature required) ───────────────────────────────────

    [Fact]
    public async Task VerifyAsync_EmptyRing_IgnoresSignature_ReturnsNoRingRequired()
    {
        var data = "unsigned content"u8.ToArray();
        var hash = ComputeSha256Hex(data);
        // Even supplying a garbage signature should be fine when ring is empty
        var bogusSignature = new byte[64];

        var result = await _sut.VerifyAsync(hash, data, PublisherUhid,
                                            ContentCategory.SocialPost, bogusSignature);

        Assert.Equal(ContentTrustStatus.NoRingRequired, result.Status);
    }

    // ── Signature verification (ECDSA P-256 as stand-in for Ed25519) ─────────

    [Fact]
    public async Task VerifyAsync_ValidSignatureFromRingKey_ReturnsVerified()
    {
        using var key   = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var spki        = key.ExportSubjectPublicKeyInfo();
        var data        = "signed media"u8.ToArray();
        var hash        = ComputeSha256Hex(data);
        var signature   = key.SignData(data, HashAlgorithmName.SHA512,
                                       DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        await _sut.AddKeyAsync(spki, ContentCategory.MediaContent, "test-key");
        var result = await _sut.VerifyAsync(hash, data, PublisherUhid,
                                            ContentCategory.MediaContent, signature);

        Assert.Equal(ContentTrustStatus.Verified, result.Status);
        Assert.True(result.IsClean);
        Assert.Equal(ContentTrustStatus.Verified, _sut.GetStatus(hash));
    }

    [Fact]
    public async Task VerifyAsync_SignatureFromUnknownKey_ReturnsSignatureFailed_AndQuarantines()
    {
        using var trustedKey  = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var attackerKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var spki              = trustedKey.ExportSubjectPublicKeyInfo();
        var data              = "legit content"u8.ToArray();
        var hash              = ComputeSha256Hex(data);
        var attackerSig       = attackerKey.SignData(data, HashAlgorithmName.SHA512,
                                                     DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        await _sut.AddKeyAsync(spki, ContentCategory.AppBinary, "trusted");
        var result = await _sut.VerifyAsync(hash, data, PublisherUhid,
                                            ContentCategory.AppBinary, attackerSig);

        Assert.Equal(ContentTrustStatus.SignatureFailed, result.Status);
        Assert.Equal(ContentTrustStatus.Quarantined, _sut.GetStatus(hash));
    }

    [Fact]
    public async Task VerifyAsync_NoSignatureProvided_RingNonEmpty_ReturnsSignatureFailed()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var spki      = key.ExportSubjectPublicKeyInfo();
        var data      = "unsigned apk"u8.ToArray();
        var hash      = ComputeSha256Hex(data);

        await _sut.AddKeyAsync(spki, ContentCategory.AppBinary, "dev-key");
        var result = await _sut.VerifyAsync(hash, data, PublisherUhid, ContentCategory.AppBinary);

        Assert.Equal(ContentTrustStatus.SignatureFailed, result.Status);
    }

    // ── ContentCategory.Any wildcard ─────────────────────────────────────────

    [Fact]
    public async Task VerifyAsync_AnyRingKey_MatchesSpecificCategory()
    {
        using var key   = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var spki        = key.ExportSubjectPublicKeyInfo();
        var data        = "cross-category content"u8.ToArray();
        var hash        = ComputeSha256Hex(data);
        var signature   = key.SignData(data, HashAlgorithmName.SHA512,
                                       DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        // Key added to Any, but content is categorised as Document
        await _sut.AddKeyAsync(spki, ContentCategory.Any, "global-key");
        var result = await _sut.VerifyAsync(hash, data, PublisherUhid,
                                            ContentCategory.Document, signature);

        Assert.Equal(ContentTrustStatus.Verified, result.Status);
    }

    // ── Ring management ───────────────────────────────────────────────────────

    [Fact]
    public async Task AddKeyAsync_Idempotent_DoesNotDuplicate()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var spki      = key.ExportSubjectPublicKeyInfo();

        await _sut.AddKeyAsync(spki, ContentCategory.AppBinary, "dup-key");
        await _sut.AddKeyAsync(spki, ContentCategory.AppBinary, "dup-key");

        Assert.Single(_sut.GetRing(ContentCategory.AppBinary));
    }

    [Fact]
    public async Task RevokeKeyAsync_RemovesKey_SubsequentVerificationFails()
    {
        using var key   = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var spki        = key.ExportSubjectPublicKeyInfo();
        var data        = "revocation test"u8.ToArray();
        var hash        = ComputeSha256Hex(data);
        var signature   = key.SignData(data, HashAlgorithmName.SHA512,
                                       DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        await _sut.AddKeyAsync(spki, ContentCategory.MediaContent, "to-revoke");
        await _sut.RevokeKeyAsync(spki, ContentCategory.MediaContent);

        Assert.Empty(_sut.GetRing(ContentCategory.MediaContent));

        // After revocation ring is empty → NoRingRequired (hash check only)
        var result = await _sut.VerifyAsync(hash, data, PublisherUhid,
                                            ContentCategory.MediaContent, signature);
        Assert.Equal(ContentTrustStatus.NoRingRequired, result.Status);
    }

    // ── Quarantine management ─────────────────────────────────────────────────

    [Fact]
    public async Task QuarantineAsync_ManualQuarantine_ReflectedInStatus()
    {
        const string hash = "aabbccdd" + "aabbccdd" + "aabbccdd" + "aabbccdd" +
                            "aabbccdd" + "aabbccdd" + "aabbccdd" + "aabbccdd";

        await _sut.QuarantineAsync(hash, "manual admin action");

        Assert.Equal(ContentTrustStatus.Quarantined, _sut.GetStatus(hash));
        Assert.Contains(hash, _sut.GetQuarantinedHashes());
    }

    [Fact]
    public async Task GetQuarantinedHashes_ReturnsOnlyQuarantinedEntries()
    {
        var dataA = "content-a"u8.ToArray();
        var dataB = "content-b"u8.ToArray();
        var hashA = ComputeSha256Hex(dataA);
        var hashB = ComputeSha256Hex(dataB);

        // hashA: clean verification (no ring)
        await _sut.VerifyAsync(hashA, dataA, PublisherUhid, ContentCategory.SocialPost);
        // hashB: wrong hash → quarantined
        await _sut.VerifyAsync("wronghash" + new string('0', 55), dataB, PublisherUhid,
                               ContentCategory.SocialPost);

        var quarantined = _sut.GetQuarantinedHashes();
        Assert.DoesNotContain(hashA, quarantined);
        // hashB's declared hash (wrong) appears in quarantine
        Assert.Single(quarantined);
    }

    [Fact]
    public void GetStatus_UnknownHash_ReturnsUnknown()
    {
        Assert.Equal(ContentTrustStatus.Unknown, _sut.GetStatus("nonexistent-hash"));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string ComputeSha256Hex(byte[] data) =>
        Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();

    public void Dispose() => _sut.Dispose();
}
