// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Sorcha.Cryptography.Interfaces;
using Sorcha.Cryptography.Enums;

namespace Sorcha.Cryptography.Utilities;

/// <summary>
/// Provides Merkle tree construction and verification for blockchain dockets
/// </summary>
public class MerkleTree
{
    private readonly IHashProvider _hashProvider;

    public MerkleTree(IHashProvider hashProvider)
    {
        _hashProvider = hashProvider ?? throw new ArgumentNullException(nameof(hashProvider));
    }

    /// <summary>
    /// Computes the Merkle root from a list of transaction hashes
    /// </summary>
    /// <param name="transactionHashes">List of transaction hashes (hex strings)</param>
    /// <param name="hashType">Hash algorithm to use (default SHA256)</param>
    /// <returns>Merkle root as hex string</returns>
    public string ComputeMerkleRoot(IReadOnlyList<string> transactionHashes, HashType hashType = HashType.SHA256)
    {
        if (transactionHashes == null || transactionHashes.Count == 0)
        {
            // Empty Merkle root (hash of empty string)
            var emptyHash = _hashProvider.ComputeHash(Array.Empty<byte>(), hashType);
            return Convert.ToHexString(emptyHash).ToLowerInvariant();
        }

        // Single transaction - return its hash
        if (transactionHashes.Count == 1)
        {
            return transactionHashes[0].ToLowerInvariant();
        }

        // Build Merkle tree from leaf hashes
        var currentLevel = transactionHashes.Select(h => h.ToLowerInvariant()).ToList();

        while (currentLevel.Count > 1)
        {
            var nextLevel = new List<string>();

            // Pair up hashes and combine them
            for (int i = 0; i < currentLevel.Count; i += 2)
            {
                string left = currentLevel[i];
                string right;

                // If odd number of hashes, duplicate the last one
                if (i + 1 < currentLevel.Count)
                {
                    right = currentLevel[i + 1];
                }
                else
                {
                    right = left;
                }

                // Combine and hash the pair
                string combined = CombineAndHash(left, right, hashType);
                nextLevel.Add(combined);
            }

            currentLevel = nextLevel;
        }

        return currentLevel[0];
    }

    /// <summary>
    /// Computes the Merkle root from a list of data items (e.g., transaction objects)
    /// </summary>
    /// <param name="dataItems">List of data items to hash</param>
    /// <param name="hashType">Hash algorithm to use (default SHA256)</param>
    /// <returns>Merkle root as hex string</returns>
    public string ComputeMerkleRootFromData(IReadOnlyList<byte[]> dataItems, HashType hashType = HashType.SHA256)
    {
        if (dataItems == null || dataItems.Count == 0)
        {
            var emptyHash = _hashProvider.ComputeHash(Array.Empty<byte>(), hashType);
            return Convert.ToHexString(emptyHash).ToLowerInvariant();
        }

        // Hash each data item to create leaf hashes
        var leafHashes = dataItems.Select(data =>
        {
            var hash = _hashProvider.ComputeHash(data, hashType);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }).ToList();

        return ComputeMerkleRoot(leafHashes, hashType);
    }

    /// <summary>
    /// Verifies that a data item is part of a Merkle tree with a given root
    /// </summary>
    /// <param name="dataHash">Hash of the data item</param>
    /// <param name="merkleRoot">Expected Merkle root</param>
    /// <param name="proof">Merkle proof (list of sibling hashes)</param>
    /// <param name="hashType">Hash algorithm used</param>
    /// <returns>True if the data is part of the tree</returns>
    public bool VerifyMerkleProof(
        string dataHash,
        string merkleRoot,
        IReadOnlyList<string> proof,
        HashType hashType = HashType.SHA256)
    {
        if (string.IsNullOrWhiteSpace(dataHash) || string.IsNullOrWhiteSpace(merkleRoot))
            return false;

        string currentHash = dataHash.ToLowerInvariant();

        // Apply each proof element
        foreach (var proofElement in proof)
        {
            // Determine order (smaller hash goes first for deterministic ordering)
            if (string.Compare(currentHash, proofElement, StringComparison.OrdinalIgnoreCase) < 0)
            {
                currentHash = CombineAndHash(currentHash, proofElement, hashType);
            }
            else
            {
                currentHash = CombineAndHash(proofElement, currentHash, hashType);
            }
        }

        return string.Equals(currentHash, merkleRoot.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Combines two hashes and computes their hash
    /// </summary>
    private string CombineAndHash(string leftHash, string rightHash, HashType hashType)
    {
        // Concatenate the two hashes
        string combined = leftHash + rightHash;
        byte[] combinedBytes = Encoding.UTF8.GetBytes(combined);

        // Hash the combined string
        byte[] resultHash = _hashProvider.ComputeHash(combinedBytes, hashType);

        return Convert.ToHexString(resultHash).ToLowerInvariant();
    }
}
