using Sorcha.WalletService.Api.Models;
using Sorcha.WalletService.Domain.Entities;

namespace Sorcha.WalletService.Api.Mappers;

/// <summary>
/// Maps between domain entities and DTOs
/// </summary>
public static class WalletMapper
{
    /// <summary>
    /// Maps Wallet entity to WalletDto
    /// </summary>
    public static WalletDto ToDto(this Wallet wallet)
    {
        return new WalletDto
        {
            Address = wallet.Address,
            Name = wallet.Name,
            PublicKey = wallet.PublicKey,
            Algorithm = wallet.Algorithm,
            Status = wallet.Status.ToString(),
            Owner = wallet.Owner,
            Tenant = wallet.Tenant,
            CreatedAt = wallet.CreatedAt,
            UpdatedAt = wallet.UpdatedAt,
            Metadata = new Dictionary<string, string>(wallet.Metadata)
        };
    }

    /// <summary>
    /// Maps WalletAccess entity to WalletAccessDto
    /// </summary>
    public static WalletAccessDto ToDto(this WalletAccess access)
    {
        return new WalletAccessDto
        {
            Id = access.Id,
            Subject = access.Subject,
            AccessRight = access.AccessRight.ToString(),
            GrantedBy = access.GrantedBy,
            GrantedAt = access.GrantedAt,
            ExpiresAt = access.ExpiresAt,
            IsActive = access.IsActive,
            Reason = access.Reason
        };
    }
}
