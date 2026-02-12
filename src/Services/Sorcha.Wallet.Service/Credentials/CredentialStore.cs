// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.EntityFrameworkCore;
using Sorcha.Wallet.Core.Data;
using Sorcha.Wallet.Core.Domain.Entities;

namespace Sorcha.Wallet.Service.Credentials;

/// <summary>
/// EF Core-backed credential store for wallet verifiable credentials.
/// </summary>
public class CredentialStore : ICredentialStore
{
    private readonly WalletDbContext _db;

    public CredentialStore(WalletDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CredentialEntity>> GetByWalletAsync(
        string walletAddress, CancellationToken ct = default)
    {
        return await _db.Credentials
            .Where(c => c.WalletAddress == walletAddress && c.Status == "Active")
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<CredentialEntity?> GetByIdAsync(string credentialId, CancellationToken ct = default)
    {
        return await _db.Credentials
            .FirstOrDefaultAsync(c => c.Id == credentialId, ct);
    }

    /// <inheritdoc />
    public async Task StoreAsync(CredentialEntity credential, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credential);

        var existing = await _db.Credentials
            .FirstOrDefaultAsync(c => c.Id == credential.Id, ct);

        if (existing != null)
        {
            _db.Entry(existing).CurrentValues.SetValues(credential);
        }
        else
        {
            _db.Credentials.Add(credential);
        }

        await _db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string credentialId, CancellationToken ct = default)
    {
        var credential = await _db.Credentials
            .FirstOrDefaultAsync(c => c.Id == credentialId, ct);

        if (credential == null)
            return false;

        _db.Credentials.Remove(credential);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateStatusAsync(string credentialId, string status, CancellationToken ct = default)
    {
        var credential = await _db.Credentials
            .FirstOrDefaultAsync(c => c.Id == credentialId, ct);

        if (credential == null)
            return false;

        credential.Status = status;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CredentialEntity>> MatchAsync(
        string walletAddress,
        string? type = null,
        IEnumerable<string>? acceptedIssuers = null,
        CancellationToken ct = default)
    {
        var query = _db.Credentials
            .Where(c => c.WalletAddress == walletAddress && c.Status == "Active");

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(c => c.Type == type);
        }

        var issuerList = acceptedIssuers?.ToList();
        if (issuerList is { Count: > 0 })
        {
            query = query.Where(c => issuerList.Contains(c.IssuerDid));
        }

        // Exclude expired credentials
        var now = DateTimeOffset.UtcNow;
        query = query.Where(c => c.ExpiresAt == null || c.ExpiresAt > now);

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);
    }
}
