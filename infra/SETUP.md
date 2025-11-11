# Azure Deployment Setup Guide

This guide will help you set up automated Azure deployments for Sorcha via GitHub Actions.

## 🎯 Quick Setup (5 minutes)

### Step 1: Create Azure Service Principal

Run this command in your terminal (requires Azure CLI):

```bash
# Login to Azure
az login

# Get your subscription ID
az account show --query id -o tsv

# Create service principal (replace {subscription-id} with your actual subscription ID)
az ad sp create-for-rbac \
  --name "sorcha-github-actions" \
  --role contributor \
  --scopes /subscriptions/{subscription-id} \
  --sdk-auth
```

This will output JSON like:

```json
{
  "clientId": "...",
  "clientSecret": "...",
  "subscriptionId": "...",
  "tenantId": "...",
  "activeDirectoryEndpointUrl": "...",
  "resourceManagerEndpointUrl": "...",
  ...
}
```

**⚠️ IMPORTANT**: Copy this entire JSON output - you'll need it in the next step!

### Step 2: Configure GitHub Secrets

1. Go to your GitHub repository
2. Click on **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Add the following secret:
   - **Name**: `AZURE_CREDENTIALS`
   - **Value**: Paste the entire JSON output from Step 1

### Step 3: Customize Deployment Settings (Optional)

Edit [`.github/workflows/azure-deploy.yml`](../.github/workflows/azure-deploy.yml) and update these variables if needed:

```yaml
env:
  AZURE_RESOURCE_GROUP: sorcha-rg        # Your resource group name
  AZURE_LOCATION: eastus                 # Your preferred Azure region
  CONTAINER_REGISTRY: sorchaacr          # Your ACR name (must be globally unique!)
```

**Important**: The `CONTAINER_REGISTRY` name must be globally unique across all of Azure. Consider using something like `sorcha{yourname}` or `sorcha{companyname}`.

### Step 4: Deploy!

Push your code to the `main` or `master` branch:

```bash
git add .
git commit -m "Add Azure deployment configuration"
git push origin main
```

The deployment will automatically start! 🚀

You can monitor the deployment progress in the **Actions** tab of your GitHub repository.

## 🌍 Azure Regions

Choose a region close to your users for best performance:

| Region | Location | Code |
|--------|----------|------|
| East US | Virginia, USA | `eastus` |
| West Europe | Netherlands | `westeurope` |
| Southeast Asia | Singapore | `southeastasia` |
| Australia East | Sydney | `australiaeast` |
| UK South | London | `uksouth` |

[See all Azure regions](https://azure.microsoft.com/en-us/explore/global-infrastructure/geographies/)

## 💰 Cost Estimates

Expected monthly costs for low-traffic application (~1000 requests/day):

| Service | SKU | Monthly Cost |
|---------|-----|--------------|
| Container Apps | Consumption (scales to zero) | $5-10 |
| Redis Cache | Basic C0 (250MB) | $16 |
| Container Registry | Basic | $5 |
| Log Analytics | Pay-as-you-go | $2-5 |
| **Total** | | **~$30-35/month** |

> For high-traffic applications, costs will increase based on usage. Container Apps charge $0.000012/vCPU-second and $0.000001/GiB-second when running.

## 🧪 Testing the Deployment

After deployment completes, you'll see URLs in the GitHub Actions output:

```
API Gateway: https://api-gateway.{random}.eastus.azurecontainerapps.io
Blazor Client: https://blazor-client.{random}.eastus.azurecontainerapps.io
```

Test the deployment:

```bash
# Test API Gateway health
curl https://api-gateway.{random}.eastus.azurecontainerapps.io/health

# Open Blazor Client in browser
open https://blazor-client.{random}.eastus.azurecontainerapps.io
```

## 🔧 Manual Deployment

If you prefer to deploy manually instead of using GitHub Actions:

### Option 1: Using PowerShell (Windows)

```powershell
cd infra
.\deploy.ps1 -Command deploy
```

### Option 2: Using Bash (Linux/Mac/WSL)

```bash
cd infra
chmod +x deploy.sh
./deploy.sh deploy
```

### Option 3: Using Azure CLI directly

```bash
# Set variables
export RESOURCE_GROUP="sorcha-rg"
export LOCATION="eastus"
export REGISTRY_NAME="sorchaacr"

# Deploy infrastructure
az deployment sub create \
  --name sorcha-deployment \
  --location $LOCATION \
  --template-file infra/main.bicep \
  --parameters resourceGroupName=$RESOURCE_GROUP \
               location=$LOCATION \
               containerRegistryName=$REGISTRY_NAME
```

## 📊 Monitoring Your Deployment

### View Logs

```bash
# Follow API Gateway logs
az containerapp logs show \
  --name api-gateway \
  --resource-group sorcha-rg \
  --follow

# View last 100 lines
az containerapp logs show \
  --name api-gateway \
  --resource-group sorcha-rg \
  --tail 100
```

### Check Resource Status

```bash
# List all Container Apps
az containerapp list \
  --resource-group sorcha-rg \
  --output table

# Check Redis status
az redis show \
  --name sorcha-redis-prod \
  --resource-group sorcha-rg
```

### View Metrics in Azure Portal

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your resource group (`sorcha-rg`)
3. Click on any Container App
4. Select **Metrics** to view performance data

## 🔒 Security Recommendations

### Production Deployments

For production use, consider these security enhancements:

1. **Use Managed Identities**
   - Remove ACR admin credentials
   - Use managed identities for authentication

2. **Enable Custom Domains**
   - Configure custom domain names
   - Use Azure-managed certificates

3. **Network Security**
   - Enable VNet integration
   - Use Private Endpoints for Redis
   - Configure Network Security Groups

4. **Secrets Management**
   - Store secrets in Azure Key Vault
   - Reference Key Vault from Container Apps

5. **Monitoring & Alerts**
   - Set up Azure Monitor alerts
   - Configure Application Insights
   - Enable diagnostic logs

### Update Bicep for Managed Identity

Replace this section in `resources.bicep`:

```bicep
registries: [
  {
    server: acr.properties.loginServer
    username: acr.listCredentials().username
    passwordSecretRef: 'acr-password'
  }
]
```

With:

```bicep
registries: [
  {
    server: acr.properties.loginServer
    identity: 'system'
  }
]
```

## 🆘 Troubleshooting

### "Container Registry name not available"

The ACR name must be globally unique. Try a different name:

```yaml
CONTAINER_REGISTRY: sorcha{yourname}acr
```

### "Service Principal creation failed"

Ensure you have sufficient permissions in your Azure subscription:

```bash
# Check your role
az role assignment list --assignee $(az account show --query user.name -o tsv) --output table
```

You need at least **Contributor** role at the subscription level.

### "Deployment failed: InvalidTemplate"

Validate your Bicep template:

```bash
az deployment sub validate \
  --location eastus \
  --template-file infra/main.bicep \
  --parameters resourceGroupName=sorcha-rg
```

### "Container App not starting"

Check the logs for errors:

```bash
az containerapp logs show --name api-gateway --resource-group sorcha-rg --tail 50
```

Common issues:
- Missing environment variables
- Invalid Redis connection string
- Port configuration mismatch
- Image pull failures

## 🧹 Cleanup / Delete Resources

To remove all Azure resources and stop incurring charges:

```bash
# Delete the entire resource group
az group delete --name sorcha-rg --yes --no-wait

# Or delete individual resources
az containerapp delete --name api-gateway --resource-group sorcha-rg --yes
```

## 📚 Next Steps

- [ ] Set up custom domain names
- [ ] Configure SSL certificates
- [ ] Enable Application Insights
- [ ] Set up Azure Monitor alerts
- [ ] Configure backup and disaster recovery
- [ ] Implement managed identities
- [ ] Set up staging environment
- [ ] Configure CI/CD for multiple environments

## 🤝 Getting Help

- **Azure Documentation**: https://docs.microsoft.com/azure/
- **Container Apps Docs**: https://docs.microsoft.com/azure/container-apps/
- **GitHub Actions Docs**: https://docs.github.com/actions
- **Bicep Documentation**: https://docs.microsoft.com/azure/azure-resource-manager/bicep/

If you encounter issues, please check the logs and GitHub Actions output first. For persistent problems, open an issue in the repository.
