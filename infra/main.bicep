// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors
//
// Main Bicep template for Sorcha Azure deployment
// Uses low-cost Azure services: Container Apps (Consumption plan), Azure Cache for Redis (Basic), and Container Registry (Basic)

targetScope = 'subscription'

@description('Name of the resource group')
param resourceGroupName string

@description('Azure region for resources')
param location string = 'eastus'

@description('Name of the container registry')
param containerRegistryName string

@description('Environment name (dev, staging, prod)')
param environment string = 'prod'

@description('MongoDB connection string for Peer Service (optional - will create Cosmos DB if not provided)')
param mongoDbConnectionString string = ''

@description('PostgreSQL administrator password (required for database creation)')
@secure()
param postgresAdminPassword string

@description('Tags to apply to all resources')
param tags object = {
  Environment: environment
  Application: 'Sorcha'
  ManagedBy: 'Bicep'
}

// Create resource group
resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

// Deploy databases first (PostgreSQL and Cosmos DB)
module databases 'databases.bicep' = {
  scope: rg
  name: 'sorcha-databases'
  params: {
    location: location
    environment: environment
    postgresAdminPassword: postgresAdminPassword
    tags: tags
  }
}

// Deploy all resources into the resource group
module resources 'resources.bicep' = {
  scope: rg
  name: 'sorcha-resources'
  params: {
    location: location
    containerRegistryName: containerRegistryName
    environment: environment
    // Use provided MongoDB connection string, or fall back to Cosmos DB
    mongoDbConnectionString: !empty(mongoDbConnectionString) ? mongoDbConnectionString : databases.outputs.mongoDbConnectionString
    walletDbConnectionString: databases.outputs.walletDbConnectionString
    tenantDbConnectionString: databases.outputs.tenantDbConnectionString
    tags: tags
  }
}

// Outputs
output resourceGroupName string = rg.name
output containerRegistryName string = resources.outputs.containerRegistryName
output containerRegistryLoginServer string = resources.outputs.containerRegistryLoginServer
output containerAppEnvironmentName string = resources.outputs.containerAppEnvironmentName
output redisCacheName string = resources.outputs.redisCacheName
output apiGatewayUrl string = resources.outputs.apiGatewayUrl
output blazorClientUrl string = resources.outputs.blazorClientUrl
