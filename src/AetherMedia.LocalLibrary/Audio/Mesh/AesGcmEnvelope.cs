// SPDX-License-Identifier: MIT

using System.Security.Cryptography;

namespace AetherMedia.LocalLibrary.Audio.Mesh;

/// <summary>
/// AES-256-GCM envelope used by every DTN-bundle producer in this library.
/// Wire layout: <c>[nonce(12)][tag(16)][cipher(N)]</c>. The same envelope
/// is used by scrobbles, bookmark + play-history sync, and any future
/// self-to-self payload — so a single shared encryption pipeline keeps the
/// key-management story honest (one user key, one envelope, one decrypt
/// path on the receiving device).
/// </summary>
public static class AesGcmEnvelope
{
    /// <summary>Required size of the symmetric key (AES-256).</summary>
    public const int KeySize = 32;

    /// <summary>Encrypt <paramref name="plaintext"/> under <paramref name="key"/>.</summary>
    public static byte[] Encrypt(byte[] key, ReadOnlySpan<byte> plaintext)
    {
        ValidateKey(key);
        var nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
        var cipher = new byte[plaintext.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];
        using var gcm = new AesGcm(key, tag.Length);
        gcm.Encrypt(nonce, plaintext, cipher, tag);

        var envelope = new byte[nonce.Length + tag.Length + cipher.Length];
        Buffer.BlockCopy(nonce,  0, envelope, 0,                       nonce.Length);
        Buffer.BlockCopy(tag,    0, envelope, nonce.Length,            tag.Length);
        Buffer.BlockCopy(cipher, 0, envelope, nonce.Length + tag.Length, cipher.Length);
        return envelope;
    }

    /// <summary>Decrypt an envelope produced by <see cref="Encrypt"/>.</summary>
    public static byte[] Decrypt(byte[] key, byte[] envelope)
    {
        ValidateKey(key);
        var nonceLen = AesGcm.NonceByteSizes.MaxSize;
        var tagLen = AesGcm.TagByteSizes.MaxSize;
        if (envelope is null || envelope.Length < nonceLen + tagLen)
            throw new FormatException("AES-GCM envelope is too short.");

        var nonce = envelope.AsSpan(0, nonceLen);
        var tag = envelope.AsSpan(nonceLen, tagLen);
        var cipher = envelope.AsSpan(nonceLen + tagLen);
        var plaintext = new byte[cipher.Length];
        using var gcm = new AesGcm(key, tagLen);
        gcm.Decrypt(nonce, cipher, tag, plaintext);
        return plaintext;
    }

    private static void ValidateKey(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length != KeySize)
            throw new ArgumentException($"Key must be {KeySize} bytes (AES-256).", nameof(key));
    }
}
