# Security Fix: Remove Secrets from Bicep Outputs

## Problem

The Bicep linter was warning:

```
WARNING: outputs-should-not-contain-secrets: Outputs should not contain secrets.
Found possible secret: function 'listKeys'
```

This warning appeared in `base-resources.bicep` because we were exposing sensitive information in the deployment outputs:

```bicep
// ❌ BAD - Exposes secrets in outputs
output redisConnectionString string = '${redisCache.properties.hostName}:6380,password=${redisCache.listKeys().primaryKey}...'
output acrPassword string = acr.listCredentials().passwords[0].value
```

### Why This Is a Security Risk

1. **Deployment Logs** - Outputs appear in deployment logs which may be stored or accessible
2. **CI/CD Logs** - GitHub Actions logs show deployment outputs
3. **Azure Portal** - Anyone with deployment read access can see outputs
4. **Audit Trails** - Outputs are logged in Azure Activity Logs

## Solution

**Remove secrets from outputs** and retrieve them securely at runtime only when needed.

### Changes Made

#### 1. Updated `base-resources.bicep`

**Before:**
```bicep
output redisConnectionString string = '...'  // ❌ Exposes Redis password
output acrUsername string = acr.listCredentials().username  // ❌ Exposes ACR credentials
output acrPassword string = acr.listCredentials().passwords[0].value  // ❌ Exposes ACR password
```

**After:**
```bicep
output redisCacheName string = redisCache.name  // ✅ Safe - just the name
output redisHostName string = redisCache.properties.hostName  // ✅ Safe - public hostname
// Note: Secrets retrieved at runtime via Azure CLI
```

#### 2. Updated `base-infra.bicep`

Removed secret outputs from the main template as well.

### How Secrets Are Now Retrieved

Secrets are retrieved securely at runtime using Azure CLI and Azure RBAC:

#### In Container Apps (Automatic)

The `container-apps.bicep` template retrieves secrets directly within the template:

```bicep
secrets: [
  {
    name: 'acr-password'
    value: acr.listCredentials().passwords[0].value  // ✅ OK - used internally, not in outputs
  }
  {
    name: 'redis-connection'
    value: '${redisCache.properties.hostName}:6380,password=${redisCache.listKeys().primaryKey}...'  // ✅ OK - internal only
  }
]
```

This is secure because:
- Values are **used internally** in the Container App configuration
- Values are **not exposed** in deployment outputs or logs
- Container Apps store them as **encrypted secrets**
- Only the Container App runtime can access them

#### In GitHub Actions (If Needed)

If we need secrets in the workflow, we retrieve them using Azure CLI with proper authentication:

```bash
# Retrieve ACR password securely
ACR_PASSWORD=$(az acr credential show \
  --name ${{ env.CONTAINER_REGISTRY }} \
  --query passwords[0].value -o tsv)

# Retrieve Redis key securely
REDIS_KEY=$(az redis list-keys \
  --name sorcha-redis-prod \
  --resource-group ${{ env.AZURE_RESOURCE_GROUP }} \
  --query primaryKey -o tsv)
```

Benefits:
- Uses Azure RBAC for access control
- Not logged in deployment outputs
- Ephemeral - only exists during workflow execution
- Can be marked as secret in GitHub Actions

## Security Best Practices Implemented

1. ✅ **No Secrets in Outputs** - Removed all secret values from Bicep outputs
2. ✅ **Runtime Retrieval** - Secrets fetched only when needed
3. ✅ **RBAC Control** - Access controlled via Azure AD authentication
4. ✅ **Encrypted Storage** - Container Apps store secrets encrypted
5. ✅ **Minimal Exposure** - Secrets never logged or displayed

## Verifying the Fix

### Check Bicep Linter

```bash
# Should now pass without warnings
az bicep build --file infra/base-resources.bicep
az bicep build --file infra/base-infra.bicep
az bicep build --file infra/container-apps.bicep
```

### Verify Deployment Outputs

After deployment, check outputs:

```bash
# View deployment outputs
az deployment sub show \
  --name sorcha-base-123 \
  --query properties.outputs

# Should only see non-sensitive values:
# - containerRegistryName
# - containerRegistryLoginServer
# - redisCacheName
# - redisHostName
# - etc.

# Should NOT see:
# - redisConnectionString ❌
# - acrPassword ❌
# - acrUsername ❌
```

### Test Secret Retrieval

Verify secrets can still be retrieved when needed:

```bash
# Get ACR credentials (requires RBAC permissions)
az acr credential show --name sorchaacr

# Get Redis keys (requires RBAC permissions)
az redis list-keys --name sorcha-redis-prod --resource-group sorcha
```

## Impact

- ✅ **No Functional Changes** - Container Apps still work the same way
- ✅ **Better Security** - Secrets no longer exposed in logs
- ✅ **Compliance** - Follows Azure security best practices
- ✅ **No Cost Impact** - Same infrastructure costs

## Additional Security Recommendations

For production deployments, consider these additional improvements:

### 1. Use Managed Identities

Replace admin credentials with managed identities:

```bicep
// Instead of ACR admin credentials
registries: [
  {
    server: acr.properties.loginServer
    identity: 'system'  // Use managed identity
  }
]
```

### 2. Use Azure Key Vault

Store secrets in Key Vault and reference them:

```bicep
secrets: [
  {
    name: 'redis-connection'
    keyVaultUrl: 'https://sorcha-kv.vault.azure.net/secrets/redis-connection'
    identity: 'system'
  }
]
```

### 3. Enable Private Endpoints

Restrict network access to resources:

```bicep
publicNetworkAccess: 'Disabled'
privateEndpointConnections: [ /* ... */ ]
```

### 4. Implement Secret Rotation

Automate secret rotation using Azure Automation or Azure Functions.

## References

- [Azure Bicep Best Practices - Secrets](https://learn.microsoft.com/azure/azure-resource-manager/bicep/best-practices#dont-output-secrets)
- [Azure Container Apps Secrets](https://learn.microsoft.com/azure/container-apps/manage-secrets)
- [Azure Key Vault Integration](https://learn.microsoft.com/azure/container-apps/manage-secrets?tabs=azure-cli#azure-key-vault)
- [Managed Identities](https://learn.microsoft.com/azure/container-apps/managed-identity)

## Rollback

If issues arise, the previous version with secrets in outputs is in git history:

```bash
# View previous version
git log --oneline infra/base-resources.bicep

# Revert if needed (not recommended)
git checkout <previous-commit> infra/base-resources.bicep
```

However, the new approach is more secure and should be kept.
