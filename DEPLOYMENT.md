# Sorcha Deployment Guide

Complete guide for deploying Sorcha blockchain platform to production.

## Table of Contents
- [Prerequisites](#prerequisites)
- [Docker Deployment](#docker-deployment)
- [Kubernetes Deployment](#kubernetes-deployment)
- [CI/CD Pipeline](#cicd-pipeline)
- [Configuration](#configuration)
- [Monitoring](#monitoring)
- [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Software
- **Docker** 20.10+ and Docker Compose 2.0+
- **.NET 10 SDK** (for development)
- **Git** for version control

### Required Secrets
For CI/CD deployment, configure these secrets in GitHub:
- `DOCKER_USERNAME` - Docker Hub username
- `DOCKER_PASSWORD` - Docker Hub access token

---

## Docker Deployment

### Quick Start

```bash
# Pull latest images
docker-compose pull

# Start all services
docker-compose up -d

# Check status
docker-compose ps

# View logs
docker-compose logs -f
```

### Access Services

| Service | URL | Description |
|---------|-----|-------------|
| API Gateway | http://localhost:8080 | Single entry point |
| Landing Page | http://localhost:8080 | System dashboard |
| Health Check | http://localhost:8080/api/health | Aggregated health |
| API Docs | http://localhost:8080/scalar/v1 | Interactive docs |

### Service Architecture

```
┌─────────────────────────────────────┐
│         Docker Network              │
├─────────────────────────────────────┤
│                                     │
│  ┌────────┐                         │
│  │ Redis  │◄───────┐                │
│  └────────┘        │                │
│       ▲            │                │
│       │            │                │
│  ┌────────────┐   │                │
│  │ Blueprint  │◄──┼─────┐          │
│  │    API     │   │     │          │
│  └────────────┘   │     │          │
│       ▲            │     │          │
│       │            │     │          │
│  ┌────────────┐   │     │          │
│  │   Peer     │◄──┼─────┤          │
│  │  Service   │   │     │          │
│  └────────────┘   │     │          │
│       ▲            │     │          │
│       │            │     │          │
│  ┌────────────┐   │     │          │
│  │    API     │◄──┴─────┴──────────┼─── External
│  │  Gateway   │:8080               │    (Port 8080)
│  └────────────┘                    │
│                                     │
└─────────────────────────────────────┘
```

### Configuration

Edit `docker-compose.yml` to customize:

```yaml
api-gateway:
  ports:
    - "8080:8080"  # Change external port
  environment:
    - ASPNETCORE_ENVIRONMENT=Production
    - Services__Blueprint__Url=http://blueprint-api:8080
```

### Scaling Services

```bash
# Scale peer service to 3 instances
docker-compose up -d --scale peer-service=3

# Scale blueprint API
docker-compose up -d --scale blueprint-api=2
```

### Data Persistence

Redis data is persisted in Docker volume:
```bash
# List volumes
docker volume ls | grep sorcha

# Backup Redis data
docker run --rm -v sorcha_redis-data:/data -v $(pwd):/backup alpine tar czf /backup/redis-backup.tar.gz /data

# Restore Redis data
docker run --rm -v sorcha_redis-data:/data -v $(pwd):/backup alpine tar xzf /backup/redis-backup.tar.gz -C /
```

---

## Kubernetes Deployment

### Prerequisites
- Kubernetes cluster (1.25+)
- kubectl configured
- Helm 3 (optional)

### Create Namespace

```bash
kubectl create namespace sorcha
```

### Deploy Redis

```yaml
# redis-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: redis
  namespace: sorcha
spec:
  replicas: 1
  selector:
    matchLabels:
      app: redis
  template:
    metadata:
      labels:
        app: redis
    spec:
      containers:
      - name: redis
        image: redis:8.2
        ports:
        - containerPort: 6379
        volumeMounts:
        - name: redis-data
          mountPath: /data
      volumes:
      - name: redis-data
        persistentVolumeClaim:
          claimName: redis-pvc
---
apiVersion: v1
kind: Service
metadata:
  name: redis
  namespace: sorcha
spec:
  selector:
    app: redis
  ports:
  - port: 6379
    targetPort: 6379
```

### Deploy Services

```yaml
# services-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: blueprint-api
  namespace: sorcha
spec:
  replicas: 2
  selector:
    matchLabels:
      app: blueprint-api
  template:
    metadata:
      labels:
        app: blueprint-api
    spec:
      containers:
      - name: blueprint-api
        image: sorcha/blueprint-api:latest
        ports:
        - containerPort: 8080
        env:
        - name: ConnectionStrings__Redis
          value: "redis:6379"
        livenessProbe:
          httpGet:
            path: /api/health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
---
apiVersion: v1
kind: Service
metadata:
  name: blueprint-api
  namespace: sorcha
spec:
  selector:
    app: blueprint-api
  ports:
  - port: 8080
    targetPort: 8080
```

### Ingress

```yaml
# ingress.yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: sorcha-ingress
  namespace: sorcha
  annotations:
    cert-manager.io/cluster-issuer: letsencrypt-prod
spec:
  tls:
  - hosts:
    - sorcha.example.com
    secretName: sorcha-tls
  rules:
  - host: sorcha.example.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: api-gateway
            port:
              number: 8080
```

### Deploy

```bash
kubectl apply -f redis-deployment.yaml
kubectl apply -f services-deployment.yaml
kubectl apply -f ingress.yaml

# Check status
kubectl get pods -n sorcha
kubectl get svc -n sorcha
```

---

## CI/CD Pipeline

### GitHub Actions Workflow

The `.github/workflows/ci-cd.yml` file defines:

1. **Build and Test** (on every push/PR)
   - Build solution
   - Run unit tests
   - Run integration tests
   - Generate coverage reports

2. **Docker Build** (on push to main)
   - Build multi-arch images
   - Push to Docker Hub
   - Tag with version

3. **Deploy** (after Docker build)
   - Deploy to production
   - Run health checks

4. **E2E Tests** (on PRs)
   - Run Playwright UI tests

### Setup CI/CD

1. **Configure Secrets**
   ```
   GitHub → Settings → Secrets → Actions
   Add:
   - DOCKER_USERNAME
   - DOCKER_PASSWORD
   ```

2. **Push to Main**
   ```bash
   git push origin main
   ```

3. **Monitor**
   ```
   GitHub → Actions tab
   View running workflows
   ```

### Manual Deployment

Trigger manual deployment:
```
GitHub → Actions → CI/CD Pipeline → Run workflow
```

---

## Configuration

### Environment Variables

**API Gateway**
```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
Services__Blueprint__Url=http://blueprint-api:8080
Services__Peer__Url=http://peer-service:8080
ConnectionStrings__Redis=redis:6379
```

**Blueprint API**
```bash
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__Redis=redis:6379
```

**Peer Service**
```bash
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__Redis=redis:6379
```

### Secrets Management

#### Docker Compose
Use `.env` file:
```bash
DOCKER_REGISTRY=docker.io
DOCKER_TAG=latest
REDIS_PASSWORD=your-secure-password
```

#### Kubernetes
Use Secrets:
```bash
kubectl create secret generic sorcha-secrets \
  --from-literal=redis-password='your-password' \
  -n sorcha
```

---

## Monitoring

### Health Checks

```bash
# Aggregated health
curl http://localhost:8080/api/health

# Individual services
curl http://localhost:8080/api/blueprint/status
curl http://localhost:8080/api/peer/status
```

### Logs

**Docker Compose**
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f api-gateway

# Last 100 lines
docker-compose logs --tail=100 api-gateway
```

**Kubernetes**
```bash
# All pods
kubectl logs -f -l app=api-gateway -n sorcha

# Specific pod
kubectl logs -f api-gateway-xxxxx -n sorcha
```

### Metrics

Access Aspire dashboard (development):
```
https://localhost:17256
```

Production metrics (configure):
- Application Insights
- Prometheus
- Grafana

---

## Troubleshooting

### Services Won't Start

**Check logs:**
```bash
docker-compose logs api-gateway
```

**Common issues:**
- Redis not ready: Wait for health check
- Port conflicts: Change ports in docker-compose.yml
- Network issues: Restart Docker

### High Memory Usage

**Check resources:**
```bash
docker stats
```

**Increase limits:**
```yaml
deploy:
  resources:
    limits:
      memory: 512M
```

### Database Connection Errors

**Verify Redis:**
```bash
docker-compose exec redis redis-cli ping
# Should return: PONG
```

### Performance Issues

1. **Scale services:**
   ```bash
   docker-compose up -d --scale blueprint-api=3
   ```

2. **Check Redis:**
   ```bash
   docker-compose exec redis redis-cli INFO stats
   ```

3. **Run performance tests:**
   ```bash
   cd tests/Sorcha.Performance.Tests
   dotnet run http://localhost:8080
   ```

---

## Backup & Recovery

### Backup

```bash
# Backup Redis data
docker run --rm \
  -v sorcha_redis-data:/data \
  -v $(pwd):/backup \
  alpine tar czf /backup/backup-$(date +%Y%m%d).tar.gz /data

# Backup configuration
cp docker-compose.yml backup/
cp -r .github/workflows backup/
```

### Restore

```bash
# Stop services
docker-compose down

# Restore Redis data
docker run --rm \
  -v sorcha_redis-data:/data \
  -v $(pwd):/backup \
  alpine tar xzf /backup/backup-20250109.tar.gz -C /

# Restart services
docker-compose up -d
```

---

## Security Best Practices

1. **Use HTTPS in production**
   - Add TLS certificates
   - Configure reverse proxy (nginx/Caddy)

2. **Secure Redis**
   - Enable authentication
   - Bind to localhost only

3. **Update regularly**
   ```bash
   docker-compose pull
   docker-compose up -d
   ```

4. **Scan images**
   ```bash
   docker scan sorcha/api-gateway:latest
   ```

5. **Limit resources**
   - Set memory/CPU limits
   - Use non-root users (already configured)

---

## Support

- **Documentation**: [README.md](README.md)
- **Troubleshooting**: [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
- **Issues**: https://github.com/yourusername/sorcha/issues
