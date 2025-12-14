// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors
//
// Azure resources for Sorcha deployment
// Optimized for low cost using:
// - Container Apps with Consumption plan (pay per request, scales to zero)
// - Azure Cache for Redis Basic C0 (cheapest tier)
// - Container Registry Basic SKU

@description('Azure region for resources')
param location string

@description('Name of the container registry')
param containerRegistryName string

@description('Environment name')
param environment string

@description('Resource tags')
param tags object

@description('MongoDB connection string for Peer Service')
param mongoDbConnectionString string = ''

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
          image: '${acr.properties.loginServer}/blueprint-api:latest'
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
        external: true // External for DNS mapping (n0.sorcha.dev)
        targetPort: 8080
        transport: 'http2'
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
        {
          name: 'mongodb-connection'
          value: !empty(mongoDbConnectionString) ? mongoDbConnectionString : 'mongodb://localhost:27017'
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'peer-service'
          image: '${acr.properties.loginServer}/peer-service:latest'
          resources: {
            cpu: json('0.5') // Increased for central node workload
            memory: '1Gi' // Increased for caching
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: environment
            }
            {
              name: 'ASPNETCORE_HTTP_PORTS'
              value: '8080'
            }
            {
              name: 'ConnectionStrings__Redis'
              secretRef: 'redis-connection'
            }
            {
              name: 'MongoDB__ConnectionString'
              secretRef: 'mongodb-connection'
            }
            {
              name: 'MongoDB__DatabaseName'
              value: 'sorcha_system_register'
            }
            {
              name: 'PeerService__CentralNode__IsCentralNode'
              value: 'true'
            }
            {
              name: 'PeerService__CentralNode__ValidateHostname'
              value: 'false'
            }
            {
              name: 'PeerService__CentralNode__Priority'
              value: '0'
            }
          ]
          volumeMounts: [
            {
              volumeName: 'peer-data'
              mountPath: '/app/data'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1 // Keep central node always running
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
      volumes: [
        {
          name: 'peer-data'
          storageType: 'EmptyDir'
        }
      ]
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
          image: '${acr.properties.loginServer}/api-gateway:latest'
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
            // YARP reverse proxy cluster destinations (MUST use HTTPS for Azure Container Apps)
            {
              name: 'ReverseProxy__Clusters__tenant-cluster__Destinations__destination1__Address'
              value: 'https://tenant-service.internal.${containerAppEnv.properties.defaultDomain}'
            }
            {
              name: 'ReverseProxy__Clusters__register-cluster__Destinations__destination1__Address'
              value: 'https://register-service.internal.${containerAppEnv.properties.defaultDomain}'
            }
            {
              name: 'ReverseProxy__Clusters__blueprint-cluster__Destinations__destination1__Address'
              value: 'https://${blueprintApi.properties.configuration.ingress.fqdn}'
            }
            {
              name: 'ReverseProxy__Clusters__wallet-cluster__Destinations__destination1__Address'
              value: 'https://wallet-service.internal.${containerAppEnv.properties.defaultDomain}'
            }
            {
              name: 'ReverseProxy__Clusters__peer-cluster__Destinations__destination1__Address'
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
          image: '${acr.properties.loginServer}/blazor-client:latest'
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
output containerRegistryName string = acr.name
output containerRegistryLoginServer string = acr.properties.loginServer
output containerAppEnvironmentName string = containerAppEnv.name
output redisCacheName string = redisCache.name
output apiGatewayUrl string = 'https://${apiGateway.properties.configuration.ingress.fqdn}'
output blazorClientUrl string = 'https://${blazorClient.properties.configuration.ingress.fqdn}'
