# Implementation Plan: Kubernetes Deployment

**Feature Branch**: `deployment-kubernetes`
**Created**: 2025-12-03
**Status**: Planning (0%)

## Summary

Kubernetes deployment provides production-grade container orchestration for the Sorcha platform. It enables auto-scaling, high availability, rolling updates, and enterprise security through network policies and secret management.

## Design Decisions

### Decision 1: Deployment per Service

**Approach**: Each microservice gets its own Kubernetes Deployment.

**Rationale**:
- Independent scaling per service
- Isolated failure domains
- Service-specific resource limits
- Clear ownership and monitoring

### Decision 2: Helm Charts

**Approach**: Use Helm charts for templated deployment.

**Rationale**:
- Parameterized configuration
- Version-controlled releases
- Environment-specific values
- Rollback support

### Decision 3: Ingress with TLS

**Approach**: Use Ingress controller (nginx/traefik) for external access.

**Rationale**:
- Centralized TLS termination
- Path-based routing
- Rate limiting at edge
- Consistent with cloud providers

### Decision 4: External Secrets Operator

**Approach**: Use External Secrets for vault integration.

**Rationale**:
- Secrets stored in vault
- Automatic sync to k8s secrets
- Rotation support
- Audit trail

## Technical Approach

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Kubernetes Cluster                        │
├─────────────────────────────────────────────────────────────┤
│  Namespace: sorcha-production                                │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  Ingress Controller (nginx/traefik)                    │  │
│  │  └── TLS termination, rate limiting                   │  │
│  └───────────────────────────────────────────────────────┘  │
│                          │                                   │
│                          ▼                                   │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  API Gateway (Deployment + Service)                    │  │
│  │  └── Replicas: 2-5 (HPA)                              │  │
│  └───────────────────────────────────────────────────────┘  │
│                          │                                   │
│         ┌────────────────┼────────────────┐                 │
│         ▼                ▼                ▼                 │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │  Blueprint  │  │   Wallet    │  │  Register   │         │
│  │  Service    │  │  Service    │  │  Service    │         │
│  │  (2-4 pods) │  │  (2-4 pods) │  │  (2-4 pods) │         │
│  └─────────────┘  └─────────────┘  └─────────────┘         │
│         │                │                │                 │
│         └────────────────┼────────────────┘                 │
│                          ▼                                   │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  StatefulSet: PostgreSQL (Primary + Read Replicas)     │  │
│  │  StatefulSet: Redis Cluster                            │  │
│  │  PersistentVolumeClaims for data durability            │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### Component Status

| Component | Status | Notes |
|-----------|--------|-------|
| Deployment Manifests | 0% | Not created |
| Service Manifests | 0% | Not created |
| Ingress Configuration | 0% | Not created |
| Helm Charts | 0% | Not created |
| HPA Configuration | 0% | Not created |
| Network Policies | 0% | Not created |
| Secret Management | 0% | Not created |
| PVC Configuration | 0% | Not created |

### Manifest Structure

```
deployment/
├── kubernetes/
│   ├── base/
│   │   ├── namespace.yaml
│   │   ├── configmaps/
│   │   ├── secrets/
│   │   └── services/
│   │       ├── api-gateway/
│   │       ├── blueprint-service/
│   │       ├── wallet-service/
│   │       ├── register-service/
│   │       ├── peer-service/
│   │       └── tenant-service/
│   ├── overlays/
│   │   ├── development/
│   │   ├── staging/
│   │   └── production/
│   └── helm/
│       └── sorcha/
│           ├── Chart.yaml
│           ├── values.yaml
│           └── templates/
```

## Dependencies

### Infrastructure Requirements

- Kubernetes 1.27+
- Container registry (ACR, ECR, GCR)
- Ingress controller (nginx-ingress, traefik)
- cert-manager for TLS
- External Secrets Operator (optional)

### Helm Dependencies

- postgresql (Bitnami)
- redis (Bitnami)
- nginx-ingress
- cert-manager

## Migration/Integration Notes

### Deployment Commands

```bash
# Apply manifests
kubectl apply -k deployment/kubernetes/overlays/production

# Helm install
helm install sorcha deployment/kubernetes/helm/sorcha \
  --namespace sorcha-production \
  --values values-production.yaml

# Check status
kubectl get pods -n sorcha-production
```

### Configuration Values

```yaml
# values-production.yaml
replicaCount:
  apiGateway: 3
  blueprintService: 2
  walletService: 2
  registerService: 2

resources:
  limits:
    cpu: "500m"
    memory: "512Mi"
  requests:
    cpu: "100m"
    memory: "128Mi"

autoscaling:
  enabled: true
  minReplicas: 2
  maxReplicas: 10
  targetCPUUtilization: 70
```

## Open Questions

1. Which Kubernetes distribution (AKS, EKS, GKE, vanilla)?
2. Service mesh required (Istio, Linkerd)?
3. GitOps deployment (ArgoCD, Flux)?
4. Multi-cluster federation needed?
