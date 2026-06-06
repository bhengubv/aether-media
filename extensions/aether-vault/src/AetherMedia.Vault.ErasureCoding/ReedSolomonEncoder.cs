// SPDX-License-Identifier: MIT
// For production, replace with a battle-tested RS library
// (e.g. BackBlaze JavaReedSolomon port or ISA-L)
namespace AetherMedia.Vault.ErasureCoding;

/// <summary>
/// A pure-managed Reed-Solomon erasure coder.
///
/// <para>
/// <b>Design note:</b> This implementation uses a Cauchy-matrix-based GF(2^8)
/// approach that is correct for all (k, m) combinations.  The XOR-only fast
/// path is used when k=1 (trivially splits one data shard and XORs m parity
/// shards from it), otherwise a systematic Cauchy matrix is used.
/// </para>
///
/// <para>
/// For production workloads consider replacing this class with a binding to
/// a battle-tested native library such as a BackBlaze JavaReedSolomon port
/// (e.g. via P/Invoke or an ISA-L wrapper) for 10-100× throughput improvement.
/// </para>
/// </summary>
public sealed class ReedSolomonEncoder : IErasureCoder
{
    // ── GF(2^8) arithmetic ─────────────────────────────────────────────────
    // Primitive polynomial: x^8 + x^4 + x^3 + x^2 + 1  (0x11D)

    private const int GfSize       = 256;
    private const int Primitive    = 0x11D;

    private static readonly byte[] ExpTable = new byte[GfSize * 2];
    private static readonly byte[] LogTable = new byte[GfSize];

    static ReedSolomonEncoder()
    {
        int x = 1;
        for (int i = 0; i < GfSize - 1; i++)
        {
            ExpTable[i] = (byte)x;
            LogTable[x] = (byte)i;
            x <<= 1;
            if ((x & GfSize) != 0) x ^= Primitive;
        }
        // Extend ExpTable so we can avoid modulo in hot paths.
        for (int i = GfSize - 1; i < GfSize * 2; i++)
            ExpTable[i] = ExpTable[i - (GfSize - 1)];
        LogTable[0] = 0; // unused; avoid reading LogTable[0]
    }

    private static byte GfMul(byte a, byte b)
    {
        if (a == 0 || b == 0) return 0;
        return ExpTable[LogTable[a] + LogTable[b]];
    }

    private static byte GfDiv(byte a, byte b)
    {
        if (b == 0) throw new DivideByZeroException("GF(256) division by zero.");
        if (a == 0) return 0;
        int logDiff = LogTable[a] - LogTable[b];
        return ExpTable[logDiff < 0 ? logDiff + GfSize - 1 : logDiff];
    }

    // ── Cauchy matrix ──────────────────────────────────────────────────────

    /// <summary>
    /// Builds a (k + m) × k Cauchy-based generator matrix in systematic form
    /// (the top k rows form the identity; the bottom m rows are the Cauchy
    /// parity rows).
    /// </summary>
    private static byte[][] BuildGeneratorMatrix(int k, int m)
    {
        int total = k + m;
        byte[][] matrix = new byte[total][];

        // Identity (data rows)
        for (int i = 0; i < k; i++)
        {
            matrix[i] = new byte[k];
            matrix[i][i] = 1;
        }

        // Cauchy parity rows: element [r,c] = 1 / (x_r XOR y_c)
        // x_r values: 0,1,...,m-1 (parity set)
        // y_c values: m, m+1,..., m+k-1 (data set)
        for (int r = 0; r < m; r++)
        {
            matrix[k + r] = new byte[k];
            byte xr = (byte)r;
            for (int c = 0; c < k; c++)
            {
                byte yc = (byte)(m + c);
                matrix[k + r][c] = GfDiv(1, (byte)(xr ^ yc));
            }
        }
        return matrix;
    }

    // ── IErasureCoder ──────────────────────────────────────────────────────

    /// <inheritdoc/>
    public byte[][] Encode(byte[] data, int k, int m)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (k <= 0) throw new ArgumentOutOfRangeException(nameof(k), "k must be > 0.");
        if (m <= 0) throw new ArgumentOutOfRangeException(nameof(m), "m must be > 0.");

        // Pad data so it divides evenly into k shards.
        int shardSize = (data.Length + k - 1) / k;
        byte[] padded = new byte[shardSize * k];
        Buffer.BlockCopy(data, 0, padded, 0, data.Length);
        // Prepend actual length in first 4 bytes of first shard for decode recovery.
        // Store length in big-endian in the front of padded buffer only when enough space.
        // We encode the real length as a 4-byte big-endian prefix inside the *last* shard
        // so we can strip padding on decode. Use a separate metadata approach: prepend 4 bytes.
        byte[] withLength = new byte[4 + padded.Length];
        withLength[0] = (byte)(data.Length >> 24);
        withLength[1] = (byte)(data.Length >> 16);
        withLength[2] = (byte)(data.Length >>  8);
        withLength[3] = (byte)(data.Length);
        Buffer.BlockCopy(padded, 0, withLength, 4, padded.Length);

        // Recalculate shardSize to include the 4-byte header.
        shardSize = (withLength.Length + k - 1) / k;
        byte[] aligned = new byte[shardSize * k];
        Buffer.BlockCopy(withLength, 0, aligned, 0, withLength.Length);

        // Split into k data shards.
        byte[][] dataShards = new byte[k][];
        for (int i = 0; i < k; i++)
        {
            dataShards[i] = new byte[shardSize];
            Buffer.BlockCopy(aligned, i * shardSize, dataShards[i], 0, shardSize);
        }

        // Fast path: k=1 → parity shards are all copies of the single data shard.
        if (k == 1)
        {
            byte[][] result = new byte[1 + m][];
            result[0] = dataShards[0];
            for (int p = 0; p < m; p++) result[1 + p] = (byte[])dataShards[0].Clone();
            return result;
        }

        // General path: compute parity shards using the Cauchy generator matrix.
        byte[][] generator = BuildGeneratorMatrix(k, m);
        byte[][] allShards = new byte[k + m][];
        for (int i = 0; i < k; i++) allShards[i] = dataShards[i];
        for (int p = 0; p < m; p++)
        {
            byte[] parityShard = new byte[shardSize];
            byte[] parityRow   = generator[k + p];
            for (int c = 0; c < k; c++)
            {
                byte coeff = parityRow[c];
                if (coeff == 0) continue;
                byte[] src = dataShards[c];
                for (int b = 0; b < shardSize; b++)
                    parityShard[b] ^= GfMul(coeff, src[b]);
            }
            allShards[k + p] = parityShard;
        }
        return allShards;
    }

    /// <inheritdoc/>
    public byte[] Decode(byte[]?[] shards, int k, int m)
    {
        ArgumentNullException.ThrowIfNull(shards);
        if (shards.Length != k + m)
            throw new ArgumentException($"Expected {k + m} shard slots but got {shards.Length}.", nameof(shards));

        // Count available shards.
        int available = shards.Count(s => s is not null);
        if (available < k)
            throw new InvalidOperationException(
                $"Cannot decode: only {available} of {k} required data shards are available.");

        // shards contain byte[] elements; .Length is the element count which equals the byte count.
#pragma warning disable CA2018 // Buffer.BlockCopy — count is byte count for byte[] shards
        int shardSize = shards.First(s => s is not null)!.Length;

        // If all k data shards are present, no matrix inversion needed.
        bool allDataPresent = true;
        for (int i = 0; i < k; i++)
        {
            if (shards[i] is null) { allDataPresent = false; break; }
        }

        byte[][] dataShards;

        if (allDataPresent)
        {
            dataShards = new byte[k][];
            for (int i = 0; i < k; i++) dataShards[i] = shards[i]!;
        }
        else
        {
            // Build the sub-system: select k available shards and their
            // corresponding rows from the generator matrix, then solve for
            // the missing data shards via Gaussian elimination.
            byte[][] generator = BuildGeneratorMatrix(k, m);

            // Collect available shard indices and data.
            var availableIndices = new List<int>(k);
            for (int i = 0; i < k + m && availableIndices.Count < k; i++)
            {
                if (shards[i] is not null) availableIndices.Add(i);
            }

            // Build k×k sub-matrix from available rows.
            byte[][] subMatrix = new byte[k][];
            byte[][] subShards = new byte[k][];
            for (int r = 0; r < k; r++)
            {
                subMatrix[r] = (byte[])generator[availableIndices[r]].Clone();
                subShards[r] = (byte[])shards[availableIndices[r]]!.Clone();
            }

            // Gaussian elimination over GF(2^8).
            for (int col = 0; col < k; col++)
            {
                // Find pivot.
                int pivot = -1;
                for (int row = col; row < k; row++)
                {
                    if (subMatrix[row][col] != 0) { pivot = row; break; }
                }
                if (pivot == -1)
                    throw new InvalidOperationException("Matrix is singular — cannot reconstruct data.");

                // Swap rows.
                if (pivot != col)
                {
                    (subMatrix[col], subMatrix[pivot]) = (subMatrix[pivot], subMatrix[col]);
                    (subShards[col], subShards[pivot]) = (subShards[pivot], subShards[col]);
                }

                // Normalise pivot row.
                byte pivotVal = subMatrix[col][col];
                for (int j = 0; j < k; j++)
                    subMatrix[col][j] = GfDiv(subMatrix[col][j], pivotVal);
                for (int b = 0; b < shardSize; b++)
                    subShards[col][b] = GfDiv(subShards[col][b], pivotVal);

                // Eliminate column entries in other rows.
                for (int row = 0; row < k; row++)
                {
                    if (row == col) continue;
                    byte factor = subMatrix[row][col];
                    if (factor == 0) continue;
                    for (int j = 0; j < k; j++)
                        subMatrix[row][j] ^= GfMul(factor, subMatrix[col][j]);
                    for (int b = 0; b < shardSize; b++)
                        subShards[row][b] ^= GfMul(factor, subShards[col][b]);
                }
            }

            dataShards = subShards;
        }

        // Reassemble the original bytes from data shards (byte[] so Length == byte count).
        byte[] assembled = new byte[k * shardSize];
        for (int i = 0; i < k; i++)
            Buffer.BlockCopy(dataShards[i], 0, assembled, i * shardSize, shardSize);

        // Extract original length from the first 4 bytes.
        int originalLength = (assembled[0] << 24) | (assembled[1] << 16) |
                             (assembled[2] <<  8) |  assembled[3];
        byte[] result = new byte[originalLength];
        Buffer.BlockCopy(assembled, 4, result, 0, originalLength);
#pragma warning restore CA2018
        return result;
    }
}
