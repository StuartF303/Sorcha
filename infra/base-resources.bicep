// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors
//
// Base Azure resources - ACR, Redis, Container App Environment, Log Analytics
// Does NOT include Container Apps (those are created after images are built)

@description('Azure region for resources')
param location string

@description('Name of the container registry')
param containerRegistryName string

@description('Environment name')
param environment string

@description('Resource tags')
param tags object

// Container Registry (Basic SKU - low cost)
resource acr 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
  name: containerRegistryName
  location: location
  tags: tags
  sku: {
    name: 'Basic' // Cheapest option: ~$5/month
  }
  properties: {
    adminUserEnabled: true
    publicNetworkAccess: 'Enabled'
  }
}

// Log Analytics Workspace for Container Apps (required)
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'sorcha-logs-${environment}'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018' // Pay-as-you-go pricing
    }
    retentionInDays: 30 // Minimum retention to save costs
  }
}

// Container Apps Environment (Consumption plan - cheapest)
resource containerAppEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: 'sorcha-env-${environment}'
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption' // Free tier, pay only for execution
      }
    ]
  }
}

// Azure Cache for Redis (Basic C0 - cheapest tier)
resource redisCache 'Microsoft.Cache/redis@2024-11-01' = {
  name: 'sorcha-redis-${environment}'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'Basic' // ~$16/month
      family: 'C'
      capacity: 0 // C0 - 250MB cache
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    redisConfiguration: {
      'maxmemory-policy': 'allkeys-lru'
    }
  }
}

// Outputs
output containerRegistryName string = acr.name
output containerRegistryLoginServer string = acr.properties.loginServer
output containerAppEnvironmentName string = containerAppEnv.name
output containerAppEnvironmentId string = containerAppEnv.id
output redisCacheName string = redisCache.name
output redisConnectionString string = '${redisCache.properties.hostName}:6380,password=${redisCache.listKeys().primaryKey},ssl=True,abortConnect=False'
output acrUsername string = acr.listCredentials().username
output acrPassword string = acr.listCredentials().passwords[0].value
