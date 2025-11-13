// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors
//
// Container Apps deployment template
// This is deployed AFTER Docker images are built and pushed to ACR

@description('Azure region for resources')
param location string

@description('Name of the container registry')
param containerRegistryName string

@description('Environment name')
param environment string = 'prod'

@description('Docker image tag to deploy')
param imageTag string = 'latest'

@description('Resource tags')
param tags object = {
  Environment: environment
  Application: 'Sorcha'
  ManagedBy: 'Bicep'
}

// Get existing resources
resource acr 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' existing = {
  name: containerRegistryName
}

resource containerAppEnv 'Microsoft.App/managedEnvironments@2024-03-01' existing = {
  name: 'sorcha-env-${environment}'
}

resource redisCache 'Microsoft.Cache/redis@2024-11-01' existing = {
  name: 'sorcha-redis-${environment}'
}

// Blueprint API Container App
resource blueprintApi 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'blueprint-api'
  location: location
  tags: tags
  properties: {
    environmentId: containerAppEnv.id
    workloadProfileName: 'Consumption'
    configuration: {
      ingress: {
        external: false // Internal only, accessed through API Gateway
        targetPort: 8080
        transport: 'http'
        allowInsecure: true
      }
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password'
        }
      ]
      secrets: [
        {
          name: 'acr-password'
          value: acr.listCredentials().passwords[0].value
        }
        {
          name: 'redis-connection'
          value: '${redisCache.properties.hostName}:6380,password=${redisCache.listKeys().primaryKey},ssl=True,abortConnect=False'
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'blueprint-api'
          image: '${acr.properties.loginServer}/blueprint-api:${imageTag}'
          resources: {
            cpu: json('0.25') // Minimal CPU
            memory: '0.5Gi' // Minimal memory
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: environment
            }
            {
              name: 'ConnectionStrings__Redis'
              secretRef: 'redis-connection'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 0 // Scale to zero when not in use
        maxReplicas: 3
        rules: [
          {
            name: 'http-rule'
            http: {
              metadata: {
                concurrentRequests: '10'
              }
            }
          }
        ]
      }
    }
  }
}

// Peer Service Container App
resource peerService 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'peer-service'
  location: location
  tags: tags
  properties: {
    environmentId: containerAppEnv.id
    workloadProfileName: 'Consumption'
    configuration: {
      ingress: {
        external: false // Internal only
        targetPort: 8080
        transport: 'http'
        allowInsecure: true
      }
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password'
        }
      ]
      secrets: [
        {
          name: 'acr-password'
          value: acr.listCredentials().passwords[0].value
        }
        {
          name: 'redis-connection'
          value: '${redisCache.properties.hostName}:6380,password=${redisCache.listKeys().primaryKey},ssl=True,abortConnect=False'
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'peer-service'
          image: '${acr.properties.loginServer}/peer-service:${imageTag}'
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: environment
            }
            {
              name: 'ConnectionStrings__Redis'
              secretRef: 'redis-connection'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 3
        rules: [
          {
            name: 'http-rule'
            http: {
              metadata: {
                concurrentRequests: '10'
              }
            }
          }
        ]
      }
    }
  }
}

// API Gateway Container App (External, publicly accessible)
resource apiGateway 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'api-gateway'
  location: location
  tags: tags
  properties: {
    environmentId: containerAppEnv.id
    workloadProfileName: 'Consumption'
    configuration: {
      ingress: {
        external: true // Publicly accessible
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
      }
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password'
        }
      ]
      secrets: [
        {
          name: 'acr-password'
          value: acr.listCredentials().passwords[0].value
        }
        {
          name: 'redis-connection'
          value: '${redisCache.properties.hostName}:6380,password=${redisCache.listKeys().primaryKey},ssl=True,abortConnect=False'
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'api-gateway'
          image: '${acr.properties.loginServer}/api-gateway:${imageTag}'
          resources: {
            cpu: json('0.5') // Slightly more for gateway
            memory: '1Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: environment
            }
            {
              name: 'ConnectionStrings__Redis'
              secretRef: 'redis-connection'
            }
            {
              name: 'Services__BlueprintApi'
              value: 'https://${blueprintApi.properties.configuration.ingress.fqdn}'
            }
            {
              name: 'Services__PeerService'
              value: 'https://${peerService.properties.configuration.ingress.fqdn}'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1 // Keep at least 1 for responsiveness
        maxReplicas: 5
        rules: [
          {
            name: 'http-rule'
            http: {
              metadata: {
                concurrentRequests: '20'
              }
            }
          }
        ]
      }
    }
  }
}

// Blazor Client Container App (External, publicly accessible)
resource blazorClient 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'blazor-client'
  location: location
  tags: tags
  properties: {
    environmentId: containerAppEnv.id
    workloadProfileName: 'Consumption'
    configuration: {
      ingress: {
        external: true // Publicly accessible
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
      }
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password'
        }
      ]
      secrets: [
        {
          name: 'acr-password'
          value: acr.listCredentials().passwords[0].value
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'blazor-client'
          image: '${acr.properties.loginServer}/blazor-client:${imageTag}'
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: environment
            }
            {
              name: 'ApiGatewayUrl'
              value: 'https://${apiGateway.properties.configuration.ingress.fqdn}'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 5
        rules: [
          {
            name: 'http-rule'
            http: {
              metadata: {
                concurrentRequests: '30'
              }
            }
          }
        ]
      }
    }
  }
}

// Outputs
output apiGatewayUrl string = 'https://${apiGateway.properties.configuration.ingress.fqdn}'
output blazorClientUrl string = 'https://${blazorClient.properties.configuration.ingress.fqdn}'
output blueprintApiUrl string = 'https://${blueprintApi.properties.configuration.ingress.fqdn}'
output peerServiceUrl string = 'https://${peerService.properties.configuration.ingress.fqdn}'
