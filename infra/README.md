# Sorcha Azure Infrastructure

This directory contains the Infrastructure as Code (IaC) for deploying Sorcha to Azure using Bicep templates.

## 🏗️ Architecture Overview

The deployment uses the following low-cost Azure services:

- **Azure Container Apps** (Consumption plan) - Serverless containers that scale to zero
- **Azure Cache for Redis** (Basic C0) - 250MB cache for distributed caching
- **Azure Container Registry** (Basic) - Store Docker images
- **Log Analytics Workspace** - Monitoring and logging

### Estimated Monthly Cost

| Service | SKU | Estimated Cost |
|---------|-----|----------------|
| Container Apps | Consumption | ~$5-15 (pay per use, scales to zero) |
| Redis Cache | Basic C0 | ~$16/month |
| Container Registry | Basic | ~$5/month |
| Log Analytics | Pay-as-you-go | ~$2-5/month |
| **Total** | | **~$30-40/month** |

> **Note**: Costs may vary based on usage. Container Apps on Consumption plan can scale to zero, significantly reducing costs during low-traffic periods.

## 📋 Prerequisites

1. **Azure Subscription** - [Create a free account](https://azure.microsoft.com/free/)
2. **Azure CLI** - [Install Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)
3. **GitHub Account** - For CI/CD pipeline
4. **Docker** - For building container images locally (optional)

## 🚀 Deployment Options

### Option 1: Automated Deployment via GitHub Actions (Recommended)

1. **Create Azure Service Principal**

```bash
# Login to Azure
az login

# Create service principal
az ad sp create-for-rbac --name "sorcha-github-actions" \
  --role contributor \
  --scopes /subscriptions/{subscription-id} \
  --sdk-auth
```

2. **Configure GitHub Secrets**

Add the following secrets to your GitHub repository (Settings → Secrets and variables → Actions):

- `AZURE_CREDENTIALS` - The JSON output from the service principal creation

3. **Customize Deployment Variables**

Edit `.github/workflows/azure-deploy.yml` and update these variables:

```yaml
env:
  AZURE_RESOURCE_GROUP: sorcha-rg  # Your resource group name
  AZURE_LOCATION: eastus           # Your preferred Azure region
  CONTAINER_REGISTRY: sorchaacr    # Your ACR name (must be globally unique)
```

4. **Push to Main Branch**

The deployment will automatically trigger when you push to `main` or `master` branch after tests pass.

### Option 2: Manual Deployment via Azure CLI

1. **Login to Azure**

```bash
az login
```

2. **Deploy Infrastructure**

```bash
# Set variables
RESOURCE_GROUP="sorcha-rg"
LOCATION="eastus"
REGISTRY_NAME="sorchaacr"  # Must be globally unique

# Deploy Bicep template
az deployment sub create \
  --name sorcha-deployment \
  --location $LOCATION \
  --template-file infra/main.bicep \
  --parameters resourceGroupName=$RESOURCE_GROUP \
               location=$LOCATION \
               containerRegistryName=$REGISTRY_NAME \
               environment=prod
```

3. **Build and Push Docker Images**

```bash
# Login to ACR
az acr login --name $REGISTRY_NAME

# Build and push images
docker build -t $REGISTRY_NAME.azurecr.io/blueprint-api:latest \
  -f src/Apps/Services/Sorcha.Blueprint.Api/Dockerfile .
docker push $REGISTRY_NAME.azurecr.io/blueprint-api:latest

docker build -t $REGISTRY_NAME.azurecr.io/api-gateway:latest \
  -f src/Apps/Services/Sorcha.ApiGateway/Dockerfile .
docker push $REGISTRY_NAME.azurecr.io/api-gateway:latest

docker build -t $REGISTRY_NAME.azurecr.io/peer-service:latest \
  -f src/Apps/Services/Sorcha.Peer.Service/Dockerfile .
docker push $REGISTRY_NAME.azurecr.io/peer-service:latest

docker build -t $REGISTRY_NAME.azurecr.io/blazor-client:latest \
  -f src/Apps/UI/Sorcha.Blueprint.Designer.Client/Dockerfile .
docker push $REGISTRY_NAME.azurecr.io/blazor-client:latest
```

4. **Get Deployment URLs**

```bash
# Get API Gateway URL
az containerapp show \
  --name api-gateway \
  --resource-group $RESOURCE_GROUP \
  --query properties.configuration.ingress.fqdn \
  -o tsv

# Get Blazor Client URL
az containerapp show \
  --name blazor-client \
  --resource-group $RESOURCE_GROUP \
  --query properties.configuration.ingress.fqdn \
  -o tsv
```

## 🔧 Configuration

### Environment Variables

The Container Apps are configured with the following environment variables:

- `ASPNETCORE_ENVIRONMENT` - Set to `prod`, `staging`, or `dev`
- `ConnectionStrings__Redis` - Redis connection string (automatically configured)
- `Services__BlueprintApi` - Internal URL for Blueprint API
- `Services__PeerService` - Internal URL for Peer Service
- `ApiGatewayUrl` - Public URL for API Gateway

### Scaling Configuration

The deployment is optimized for low cost with the following scaling rules:

- **Blueprint API, Peer Service, Blazor Client**: Scale from 0 to 3 replicas
- **API Gateway**: Scale from 1 to 5 replicas (kept at min 1 for responsiveness)

Scaling is triggered by HTTP concurrency:
- Internal services: 10 concurrent requests
- Gateway: 20 concurrent requests
- Blazor client: 30 concurrent requests

## 📊 Monitoring

### View Logs

```bash
# View API Gateway logs
az containerapp logs show \
  --name api-gateway \
  --resource-group $RESOURCE_GROUP \
  --follow

# View all Container Apps
az containerapp list \
  --resource-group $RESOURCE_GROUP \
  --output table
```

### Monitor Metrics

```bash
# Check scaling metrics
az monitor metrics list \
  --resource "/subscriptions/{subscription-id}/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.App/containerApps/api-gateway" \
  --metric "Requests" \
  --output table
```

## 🔒 Security Best Practices

1. **Use Managed Identities** - Update the Bicep templates to use managed identities instead of ACR admin credentials
2. **Enable Custom Domains** - Configure custom domains with SSL certificates for production
3. **Network Isolation** - Consider using VNet integration for enhanced security
4. **Secrets Management** - Use Azure Key Vault for sensitive configuration
5. **Enable Diagnostic Logs** - Send logs to Azure Monitor for analysis

## 💰 Cost Optimization Tips

1. **Scale to Zero** - Container Apps automatically scale to zero during idle periods
2. **Use Reserved Capacity** - For consistent workloads, consider reserved instances
3. **Monitor Usage** - Regularly check Azure Cost Management
4. **Right-size Resources** - Adjust CPU/memory allocations based on actual usage
5. **Clean Up Unused Resources** - Remove test/dev environments when not needed

## 🧹 Cleanup

To remove all deployed resources:

```bash
# Delete resource group (removes all resources)
az group delete --name $RESOURCE_GROUP --yes --no-wait
```

## 🆘 Troubleshooting

### Container App not starting

```bash
# Check container app status
az containerapp show --name api-gateway --resource-group $RESOURCE_GROUP

# View recent logs
az containerapp logs show --name api-gateway --resource-group $RESOURCE_GROUP --tail 50
```

### Redis connection issues

```bash
# Test Redis connectivity
az redis show --name sorcha-redis-prod --resource-group $RESOURCE_GROUP
az redis list-keys --name sorcha-redis-prod --resource-group $RESOURCE_GROUP
```

### Deployment failures

```bash
# View deployment operations
az deployment sub show --name sorcha-deployment
```

## 📚 Additional Resources

- [Azure Container Apps Documentation](https://learn.microsoft.com/azure/container-apps/)
- [Azure Cache for Redis Documentation](https://learn.microsoft.com/azure/azure-cache-for-redis/)
- [Bicep Documentation](https://learn.microsoft.com/azure/azure-resource-manager/bicep/)
- [Azure Pricing Calculator](https://azure.microsoft.com/pricing/calculator/)
