// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors
//
// Base infrastructure template - Creates Resource Group, ACR, Redis, and Container App Environment
// This is deployed FIRST, before building Docker images

targetScope = 'subscription'

@description('Name of the resource group')
param resourceGroupName string

@description('Azure region for resources')
param location string = 'uksouth'

@description('Name of the container registry')
param containerRegistryName string

@description('Environment name (dev, staging, prod)')
param environment string = 'prod'

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

// Deploy base resources (ACR, Redis, Environment, Log Analytics)
module baseResources 'base-resources.bicep' = {
  scope: rg
  name: 'sorcha-base-resources'
  params: {
    location: location
    containerRegistryName: containerRegistryName
    environment: environment
    tags: tags
  }
}

// Outputs
output resourceGroupName string = rg.name
output containerRegistryName string = baseResources.outputs.containerRegistryName
output containerRegistryLoginServer string = baseResources.outputs.containerRegistryLoginServer
output containerAppEnvironmentName string = baseResources.outputs.containerAppEnvironmentName
output containerAppEnvironmentId string = baseResources.outputs.containerAppEnvironmentId
output redisCacheName string = baseResources.outputs.redisCacheName
output redisHostName string = baseResources.outputs.redisHostName
