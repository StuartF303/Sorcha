-- SPDX-License-Identifier: MIT
-- Copyright (c) 2025 Sorcha Contributors

-- PostgreSQL initialization script
-- Creates databases for each Sorcha service

-- Tenant Service database
CREATE DATABASE sorcha_tenant;

-- Wallet Service database (uses default 'sorcha' database)
-- Blueprint Service uses Redis only
-- Register Service uses MongoDB
-- Peer Service uses Redis only

-- Grant permissions (the 'sorcha' user is already the owner)
\c sorcha_tenant
GRANT ALL PRIVILEGES ON SCHEMA public TO sorcha;
