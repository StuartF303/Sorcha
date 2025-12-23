// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors
//
// Database resources for Sorcha Azure deployment
// - Azure Database for PostgreSQL Flexible Server (for Wallet and Tenant services)
// - Azure Cosmos DB for MongoDB API (for Register and Peer services)

@description('Azure region for resources')
param location string

@description('Environment name')
param environment string

@description('Resource tags')
param tags object

@description('PostgreSQL administrator login')
@secure()
param postgresAdminUsername string = 'sorcha_admin'

@description('PostgreSQL administrator password')
@secure()
param postgresAdminPassword string

@description('Allow Azure services to access PostgreSQL (needed for Container Apps)')
param allowAzureServicesAccess bool = true

// ============================================================================
// Azure Database for PostgreSQL Flexible Server
// ============================================================================
// Burstable B1ms tier (~$12/month) - suitable for dev/staging
// For production, consider General Purpose D-series

resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-12-01-preview' = {
  name: 'sorcha-postgres-${environment}'
  location: location
  tags: tags
  sku: {
    name: 'Standard_B1ms' // Burstable: 1 vCore, 2GB RAM (~$12/month)
    tier: 'Burstable'
  }
  properties: {
    version: '17' // Use PostgreSQL 17 (latest)
    administratorLogin: postgresAdminUsername
    administratorLoginPassword: postgresAdminPassword
    storage: {
      storageSizeGB: 32 // Minimum size
      autoGrow: 'Enabled'
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled' // Disabled for cost savings
    }
    highAvailability: {
      mode: 'Disabled' // Disabled for cost savings (enable for production)
    }
    availabilityZone: '1'
  }

  // Allow access from Azure services (Container Apps)
  resource firewallRuleAzure 'firewallRules' = if (allowAzureServicesAccess) {
    name: 'AllowAzureServices'
    properties: {
      startIpAddress: '0.0.0.0'
      endIpAddress: '0.0.0.0'
    }
  }
}

// Create sorcha_wallet database
resource walletDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-12-01-preview' = {
  parent: postgresServer
  name: 'sorcha_wallet'
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

// Create sorcha_tenant database
resource tenantDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-12-01-preview' = {
  parent: postgresServer
  name: 'sorcha_tenant'
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

// ============================================================================
// Azure Cosmos DB for MongoDB API
// ============================================================================
// Serverless tier (pay-per-request) - best for dev/staging
// For production with predictable workload, consider Provisioned tier

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-08-15' = {
  name: 'sorcha-cosmos-${environment}'
  location: location
  tags: tags
  kind: 'MongoDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    enableAutomaticFailover: false
    enableFreeTier: false // Set to true for dev (one free account per subscription)
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session' // Balanced consistency
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    capabilities: [
      {
        name: 'EnableServerless' // Serverless = pay per request (cheapest for low usage)
      }
      {
        name: 'EnableMongo' // MongoDB API compatibility
      }
    ]
    apiProperties: {
      serverVersion: '6.0' // MongoDB 6.0 compatibility
    }
  }
}

// Create MongoDB database for Register Service
resource mongoDatabase 'Microsoft.DocumentDB/databaseAccounts/mongodbDatabases@2024-08-15' = {
  parent: cosmosAccount
  name: 'sorcha_system_register'
  properties: {
    resource: {
      id: 'sorcha_system_register'
    }
  }
}

// Create collection for transactions (Register Service)
resource transactionsCollection 'Microsoft.DocumentDB/databaseAccounts/mongodbDatabases/collections@2024-08-15' = {
  parent: mongoDatabase
  name: 'transactions'
  properties: {
    resource: {
      id: 'transactions'
      shardKey: {
        _id: 'Hash' // Shard by transaction ID
      }
      indexes: [
        {
          key: {
            keys: ['_id']
          }
        }
        {
          key: {
            keys: ['timestamp']
          }
        }
        {
          key: {
            keys: ['walletAddress']
          }
        }
      ]
    }
  }
}

// ============================================================================
// Outputs
// ============================================================================

output postgresServerName string = postgresServer.name
output postgresServerFqdn string = postgresServer.properties.fullyQualifiedDomainName

// PostgreSQL connection strings (for Container Apps environment variables)
output walletDbConnectionString string = 'Host=${postgresServer.properties.fullyQualifiedDomainName};Database=sorcha_wallet;Username=${postgresAdminUsername};Password=${postgresAdminPassword};SslMode=Require'
output tenantDbConnectionString string = 'Host=${postgresServer.properties.fullyQualifiedDomainName};Database=sorcha_tenant;Username=${postgresAdminUsername};Password=${postgresAdminPassword};SslMode=Require'

output cosmosAccountName string = cosmosAccount.name
output cosmosAccountEndpoint string = cosmosAccount.properties.documentEndpoint

// MongoDB connection string (for Container Apps)
output mongoDbConnectionString string = cosmosAccount.listConnectionStrings().connectionStrings[0].connectionString

// Note: In production, use Azure Key Vault to store connection strings
// These outputs should be used to populate Key Vault secrets, then
// Container Apps should reference secrets from Key Vault
