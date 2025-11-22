// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Tenant.Service.Models;

namespace Sorcha.Tenant.Service.Data.Repositories;

/// <summary>
/// Repository interface for Organization entity operations.
/// Provides CRUD operations and queries for organizations.
/// </summary>
public interface IOrganizationRepository
{
    /// <summary>
    /// Gets an organization by ID.
    /// </summary>
    Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an organization by subdomain.
    /// </summary>
    Task<Organization?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active organizations.
    /// </summary>
    Task<List<Organization>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all organizations (including suspended and deleted).
    /// </summary>
    Task<List<Organization>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new organization.
    /// </summary>
    Task<Organization> CreateAsync(Organization organization, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing organization.
    /// </summary>
    Task<Organization> UpdateAsync(Organization organization, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes an organization (sets status to Deleted).
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a subdomain is already taken.
    /// </summary>
    Task<bool> SubdomainExistsAsync(string subdomain, CancellationToken cancellationToken = default);
}
