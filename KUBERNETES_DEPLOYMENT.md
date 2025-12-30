# Kubernetes Deployment Guide for DocN

## Overview
This guide provides complete Kubernetes deployment configurations for DocN with monitoring, caching, and background job processing.

## Prerequisites
- Kubernetes cluster (1.25+)
- kubectl configured
- Helm 3.x installed
- SQL Server or Azure SQL Database
- Redis (optional, for distributed cache)

## Architecture

```
┌─────────────────┐
│   Ingress/LB   │
└────────┬────────┘
         │
    ┌────┴─────┐
    │  Service │
    └────┬─────┘
         │
┌────────┴─────────┐
│  DocN Server Pod │
│  - Serilog logs  │
│  - OpenTelemetry │
│  - Health checks │
│  - Hangfire      │
└──────────────────┘
         │
    ┌────┴──────┐
    │  Storage  │
    │  - Redis  │
    │  - SQL    │
    │  - Files  │
    └───────────┘
```

## 1. Namespace

```yaml
# namespace.yaml
apiVersion: v1
kind: Namespace
metadata:
  name: docn
  labels:
    name: docn
```

## 2. ConfigMap

```yaml
# configmap.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: docn-config
  namespace: docn
data:
  appsettings.json: |
    {
      "Serilog": {
        "MinimumLevel": {
          "Default": "Information",
          "Override": {
            "Microsoft": "Warning",
            "System": "Warning"
          }
        }
      },
      "OpenTelemetry": {
        "OtlpEndpoint": "http://jaeger-collector:4317"
      },
      "FileStorage": {
        "UploadPath": "/app/uploads"
      }
    }
```

## 3. Secrets

```yaml
# secrets.yaml
apiVersion: v1
kind: Secret
metadata:
  name: docn-secrets
  namespace: docn
type: Opaque
stringData:
  connection-string: "Server=sql-server;Database=DocN;User=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
  redis-connection: "redis-master:6379,password=YOUR_REDIS_PASSWORD"
  openai-key: "YOUR_OPENAI_KEY"
  gemini-key: "YOUR_GEMINI_KEY"
```

**Note**: In production, use external secret management (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault).

## 4. Persistent Volume Claims

```yaml
# pvc.yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: docn-uploads
  namespace: docn
spec:
  accessModes:
    - ReadWriteMany
  resources:
    requests:
      storage: 100Gi
  storageClassName: azurefile  # Or your storage class

---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: docn-logs
  namespace: docn
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 20Gi
  storageClassName: standard  # Or your storage class
```

## 5. Deployment

```yaml
# deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: docn-server
  namespace: docn
  labels:
    app: docn-server
spec:
  replicas: 3  # Horizontal scaling
  selector:
    matchLabels:
      app: docn-server
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
  template:
    metadata:
      labels:
        app: docn-server
      annotations:
        prometheus.io/scrape: "true"
        prometheus.io/port: "5211"
        prometheus.io/path: "/metrics"
    spec:
      containers:
      - name: docn-server
        image: your-registry/docn-server:latest
        imagePullPolicy: Always
        ports:
        - name: http
          containerPort: 5211
          protocol: TCP
        - name: metrics
          containerPort: 5211
          protocol: TCP
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ASPNETCORE_URLS
          value: "http://+:5211"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: docn-secrets
              key: connection-string
        - name: ConnectionStrings__Redis
          valueFrom:
            secretKeyRef:
              name: docn-secrets
              key: redis-connection
        - name: OpenAI__ApiKey
          valueFrom:
            secretKeyRef:
              name: docn-secrets
              key: openai-key
        - name: Gemini__ApiKey
          valueFrom:
            secretKeyRef:
              name: docn-secrets
              key: gemini-key
        volumeMounts:
        - name: uploads
          mountPath: /app/uploads
        - name: logs
          mountPath: /app/logs
        - name: config
          mountPath: /app/appsettings.Production.json
          subPath: appsettings.json
        resources:
          requests:
            memory: "512Mi"
            cpu: "500m"
          limits:
            memory: "2Gi"
            cpu: "2000m"
        livenessProbe:
          httpGet:
            path: /health/live
            port: 5211
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 5211
          initialDelaySeconds: 15
          periodSeconds: 5
          timeoutSeconds: 3
          failureThreshold: 3
        startupProbe:
          httpGet:
            path: /health/live
            port: 5211
          initialDelaySeconds: 0
          periodSeconds: 5
          timeoutSeconds: 3
          failureThreshold: 30
      volumes:
      - name: uploads
        persistentVolumeClaim:
          claimName: docn-uploads
      - name: logs
        persistentVolumeClaim:
          claimName: docn-logs
      - name: config
        configMap:
          name: docn-config
```

## 6. Service

```yaml
# service.yaml
apiVersion: v1
kind: Service
metadata:
  name: docn-server
  namespace: docn
  labels:
    app: docn-server
spec:
  type: ClusterIP
  ports:
  - port: 80
    targetPort: 5211
    protocol: TCP
    name: http
  - port: 5211
    targetPort: 5211
    protocol: TCP
    name: metrics
  selector:
    app: docn-server
```

## 7. Horizontal Pod Autoscaler

```yaml
# hpa.yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: docn-server-hpa
  namespace: docn
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: docn-server
  minReplicas: 3
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
  behavior:
    scaleDown:
      stabilizationWindowSeconds: 300
      policies:
      - type: Percent
        value: 50
        periodSeconds: 60
    scaleUp:
      stabilizationWindowSeconds: 0
      policies:
      - type: Percent
        value: 100
        periodSeconds: 30
      - type: Pods
        value: 2
        periodSeconds: 30
      selectPolicy: Max
```

## 8. Ingress

```yaml
# ingress.yaml
apiVersion: networking.k8.io/v1
kind: Ingress
metadata:
  name: docn-ingress
  namespace: docn
  annotations:
    kubernetes.io/ingress.class: nginx
    cert-manager.io/cluster-issuer: letsencrypt-prod
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
    nginx.ingress.kubernetes.io/proxy-body-size: "100m"
spec:
  tls:
  - hosts:
    - docn.yourdomain.com
    secretName: docn-tls
  rules:
  - host: docn.yourdomain.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: docn-server
            port:
              number: 80
```

## 9. Redis (Optional)

If using Redis for distributed cache:

```bash
# Using Bitnami Helm Chart
helm repo add bitnami https://charts.bitnami.com/bitnami
helm repo update

helm install redis bitnami/redis \
  --namespace docn \
  --set auth.password=YOUR_REDIS_PASSWORD \
  --set master.persistence.size=10Gi \
  --set replica.replicaCount=2 \
  --set replica.persistence.size=10Gi
```

## 10. Monitoring Stack

### Prometheus

```yaml
# servicemonitor.yaml
apiVersion: monitoring.coreos.com/v1
kind: ServiceMonitor
metadata:
  name: docn-server
  namespace: docn
  labels:
    app: docn-server
spec:
  selector:
    matchLabels:
      app: docn-server
  endpoints:
  - port: metrics
    path: /metrics
    interval: 15s
```

### Jaeger (Optional)

```bash
# Using Jaeger Operator
kubectl create namespace observability
kubectl create -f https://github.com/jaegertracing/jaeger-operator/releases/download/v1.51.0/jaeger-operator.yaml -n observability

# Create Jaeger instance
cat <<EOF | kubectl apply -f -
apiVersion: jaegertracing.io/v1
kind: Jaeger
metadata:
  name: jaeger
  namespace: docn
spec:
  strategy: production
  storage:
    type: elasticsearch
    options:
      es:
        server-urls: http://elasticsearch:9200
EOF
```

## 11. Deployment Commands

```bash
# Create namespace
kubectl apply -f namespace.yaml

# Create secrets
kubectl apply -f secrets.yaml

# Create config
kubectl apply -f configmap.yaml

# Create PVCs
kubectl apply -f pvc.yaml

# Deploy application
kubectl apply -f deployment.yaml
kubectl apply -f service.yaml
kubectl apply -f ingress.yaml

# Setup autoscaling
kubectl apply -f hpa.yaml

# Verify deployment
kubectl get pods -n docn
kubectl get svc -n docn
kubectl logs -f deployment/docn-server -n docn

# Check health
kubectl port-forward svc/docn-server 8080:80 -n docn
curl http://localhost:8080/health
```

## 12. Monitoring

### View Logs
```bash
# Tail logs from all pods
kubectl logs -f -l app=docn-server -n docn

# View logs from specific pod
kubectl logs -f docn-server-xxxxx -n docn

# View previous crashed pod logs
kubectl logs --previous docn-server-xxxxx -n docn
```

### Check Metrics
```bash
# Port forward to metrics endpoint
kubectl port-forward svc/docn-server 5211:5211 -n docn
curl http://localhost:5211/metrics
```

### Access Hangfire Dashboard
```bash
# Port forward to Hangfire
kubectl port-forward svc/docn-server 8080:80 -n docn
# Open http://localhost:8080/hangfire
```

## 13. Scaling

### Manual Scaling
```bash
# Scale to 5 replicas
kubectl scale deployment docn-server --replicas=5 -n docn

# Check scaling status
kubectl get hpa -n docn
```

### Cluster Autoscaler
Ensure your Kubernetes cluster has cluster autoscaler enabled to automatically scale nodes when HPA scales pods beyond node capacity.

## 14. Backup & Disaster Recovery

### Database Backup
```bash
# Create CronJob for database backup
apiVersion: batch/v1
kind: CronJob
metadata:
  name: docn-db-backup
  namespace: docn
spec:
  schedule: "0 2 * * *"  # Daily at 2 AM
  jobTemplate:
    spec:
      template:
        spec:
          containers:
          - name: backup
            image: mcr.microsoft.com/mssql-tools
            command:
            - /bin/bash
            - -c
            - |
              sqlcmd -S $DB_SERVER -U $DB_USER -P $DB_PASSWORD \
                -Q "BACKUP DATABASE DocN TO DISK='/backup/docn-$(date +%Y%m%d).bak'"
            env:
            - name: DB_SERVER
              value: "sql-server"
            - name: DB_USER
              value: "sa"
            - name: DB_PASSWORD
              valueFrom:
                secretKeyRef:
                  name: docn-secrets
                  key: db-password
            volumeMounts:
            - name: backup
              mountPath: /backup
          volumes:
          - name: backup
            persistentVolumeClaim:
              claimName: docn-backup
          restartPolicy: OnFailure
```

## 15. Troubleshooting

### Pod Not Starting
```bash
# Check pod events
kubectl describe pod docn-server-xxxxx -n docn

# Check logs
kubectl logs docn-server-xxxxx -n docn

# Check resource constraints
kubectl top pods -n docn
```

### Health Check Failures
```bash
# Test health endpoint directly
kubectl exec -it docn-server-xxxxx -n docn -- curl http://localhost:5211/health

# Check readiness
kubectl get pods -n docn -o wide
```

### Performance Issues
```bash
# Check HPA status
kubectl get hpa -n docn

# Check metrics
kubectl top pods -n docn
kubectl top nodes

# View Prometheus metrics
kubectl port-forward -n monitoring svc/prometheus 9090:9090
# Open http://localhost:9090
```

## 16. Security Best Practices

1. **Use Security Contexts**:
```yaml
securityContext:
  runAsNonRoot: true
  runAsUser: 1000
  fsGroup: 1000
  capabilities:
    drop:
    - ALL
  readOnlyRootFilesystem: true
```

2. **Network Policies**:
```yaml
apiVersion: networking.k8.io/v1
kind: NetworkPolicy
metadata:
  name: docn-network-policy
  namespace: docn
spec:
  podSelector:
    matchLabels:
      app: docn-server
  policyTypes:
  - Ingress
  - Egress
  ingress:
  - from:
    - namespaceSelector:
        matchLabels:
          name: ingress-nginx
    ports:
    - protocol: TCP
      port: 5211
  egress:
  - to:
    - namespaceSelector: {}
    ports:
    - protocol: TCP
      port: 1433  # SQL Server
    - protocol: TCP
      port: 6379  # Redis
```

3. **Pod Security Standards**: Use restricted PSS for production
4. **Image Scanning**: Scan images for vulnerabilities before deployment
5. **RBAC**: Use least privilege principle

## 17. Cost Optimization

1. **Right-size resources**: Start with smaller requests/limits
2. **Use spot/preemptible instances**: For non-critical workloads
3. **Implement Pod Disruption Budgets**:
```yaml
apiVersion: policy/v1
kind: PodDisruptionBudget
metadata:
  name: docn-pdb
  namespace: docn
spec:
  minAvailable: 2
  selector:
    matchLabels:
      app: docn-server
```

---

**Document Version**: 1.0  
**Date**: December 2024  
**Kubernetes Version**: 1.25+
