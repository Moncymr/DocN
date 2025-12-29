# Deployment Guide - DocN

## 🚀 Guida Completa al Deployment

Questa guida copre il deployment di DocN in vari ambienti: sviluppo, staging e produzione.

## 📋 Prerequisiti

### Software Richiesto
- .NET 10.0 SDK o superiore
- SQL Server 2025 o Azure SQL Database
- Web server (IIS, Nginx, Apache, o Azure App Service)
- (Opzionale) Docker e Kubernetes per deployment containerizzato

### Risorse Minime

**Development:**
- CPU: 2 cores
- RAM: 4 GB
- Storage: 20 GB
- Network: Connessione internet

**Production (piccola scala - <100 utenti):**
- CPU: 4 cores
- RAM: 8 GB
- Storage: 100 GB SSD
- Network: 100 Mbps

**Production (media scala - <1000 utenti):**
- CPU: 8 cores
- RAM: 16 GB
- Storage: 500 GB SSD
- Network: 1 Gbps

**Production (grande scala - >1000 utenti):**
- CPU: 16+ cores
- RAM: 32+ GB
- Storage: 1+ TB SSD
- Network: 10 Gbps
- Load Balancer
- Redis cache
- Message queue (RabbitMQ/Azure Service Bus)

---

## 🏗️ Architetture di Deployment

### 1. Single Server (Small/Development)

```
┌─────────────────────────────────┐
│      Single Server              │
│                                 │
│  ┌──────────────────────────┐  │
│  │  IIS / Kestrel           │  │
│  │  DocN.Server (API)       │  │
│  │  DocN.Client (Static)    │  │
│  └──────────────────────────┘  │
│                                 │
│  ┌──────────────────────────┐  │
│  │  SQL Server 2025         │  │
│  └──────────────────────────┘  │
│                                 │
│  ┌──────────────────────────┐  │
│  │  File Storage (Local)    │  │
│  └──────────────────────────┘  │
└─────────────────────────────────┘
```

**Pro:**
- Semplice da gestire
- Costi bassi
- Ideale per sviluppo/testing

**Contro:**
- Single point of failure
- Scalabilità limitata
- Performance limitata

### 2. Multi-Tier (Medium)

```
┌──────────────┐     ┌──────────────┐
│ Load         │────▶│ Web Server 1 │
│ Balancer     │     │ (App)        │
│              │     └──────────────┘
│              │     ┌──────────────┐
│              │────▶│ Web Server 2 │
└──────────────┘     │ (App)        │
                     └──────────────┘
                            │
                     ┌──────────────┐
                     │ SQL Server   │
                     │ (Database)   │
                     └──────────────┘
                            │
                     ┌──────────────┐
                     │ File Storage │
                     │ (Shared/NAS) │
                     └──────────────┘
```

**Pro:**
- Alta disponibilità
- Scalabilità orizzontale
- Performance migliorate

**Contro:**
- Complessità maggiore
- Costi più alti
- Richiede gestione

### 3. Cloud-Native (Large/Enterprise)

```
┌────────────────────────────────────────────┐
│              Azure Front Door              │
│         (CDN + WAF + Load Balancer)        │
└────────────────────────────────────────────┘
                     │
        ┌────────────┴────────────┐
        │                         │
┌───────▼────────┐      ┌────────▼────────┐
│ App Service 1  │      │ App Service 2   │
│ (Auto-scale)   │      │ (Auto-scale)    │
└────────────────┘      └─────────────────┘
        │                         │
        └────────────┬────────────┘
                     │
        ┌────────────▼────────────┐
        │   Azure SQL Database    │
        │   (with replicas)       │
        └─────────────────────────┘
                     │
        ┌────────────┴────────────┐
        │                         │
┌───────▼────────┐      ┌────────▼────────┐
│ Blob Storage   │      │ Redis Cache     │
│ (Documents)    │      │ (Distributed)   │
└────────────────┘      └─────────────────┘
        │
┌───────▼────────┐
│ Key Vault      │
│ (Secrets)      │
└────────────────┘
```

**Pro:**
- Massima scalabilità
- Alta disponibilità (99.99%)
- Gestione automatizzata
- Disaster recovery
- Security built-in

**Contro:**
- Costi più elevati
- Vendor lock-in potenziale
- Complessità operativa

---

## 🐳 Deployment con Docker

### Dockerfile

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["DocN.Server/DocN.Server.csproj", "DocN.Server/"]
COPY ["DocN.Client/DocN.Client.csproj", "DocN.Client/"]
COPY ["DocN.Data/DocN.Data.csproj", "DocN.Data/"]
COPY ["DocN.Core/DocN.Core.csproj", "DocN.Core/"]
RUN dotnet restore "DocN.Server/DocN.Server.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/DocN.Server"
RUN dotnet build "DocN.Server.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "DocN.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install Tesseract OCR
RUN apt-get update && \
    apt-get install -y tesseract-ocr libtesseract-dev libleptonica-dev && \
    rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=publish /app/publish .

# Copy tessdata
COPY DocN.Client/tessdata ./tessdata

# Expose ports
EXPOSE 8080
EXPOSE 8081

ENTRYPOINT ["dotnet", "DocN.Server.dll"]
```

### docker-compose.yml

```yaml
version: '3.8'

services:
  docn-app:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
      - "8081:8081"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080;https://+:8081
      - ConnectionStrings__DefaultConnection=Server=sql-server;Database=DocN;User Id=sa;Password=${SQL_PASSWORD};TrustServerCertificate=True;
      - Gemini__ApiKey=${GEMINI_API_KEY}
      - OpenAI__ApiKey=${OPENAI_API_KEY}
    depends_on:
      - sql-server
      - redis
    volumes:
      - ./uploads:/app/uploads
    restart: unless-stopped

  sql-server:
    image: mcr.microsoft.com/mssql/server:2025-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=${SQL_PASSWORD}
      - MSSQL_PID=Developer
    ports:
      - "1433:1433"
    volumes:
      - sqldata:/var/opt/mssql
    restart: unless-stopped

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redisdata:/data
    restart: unless-stopped

volumes:
  sqldata:
  redisdata:
```

### .env.example

```bash
SQL_PASSWORD=CHANGE_THIS_STRONG_PASSWORD_HERE
GEMINI_API_KEY=your-gemini-api-key-here
OPENAI_API_KEY=your-openai-api-key-here
```

### Comandi Docker

```bash
# Build
docker-compose build

# Start
docker-compose up -d

# View logs
docker-compose logs -f docn-app

# Stop
docker-compose down

# Rebuild and restart
docker-compose up -d --build
```

---

## ☸️ Deployment Kubernetes

### deployment.yaml

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: docn-app
  labels:
    app: docn
spec:
  replicas: 3
  selector:
    matchLabels:
      app: docn
  template:
    metadata:
      labels:
        app: docn
    spec:
      containers:
      - name: docn
        image: your-registry/docn:latest
        ports:
        - containerPort: 8080
        - containerPort: 8081
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: docn-secrets
              key: connection-string
        - name: Gemini__ApiKey
          valueFrom:
            secretKeyRef:
              name: docn-secrets
              key: gemini-api-key
        resources:
          requests:
            memory: "1Gi"
            cpu: "500m"
          limits:
            memory: "2Gi"
            cpu: "1000m"
        livenessProbe:
          httpGet:
            path: /health/live
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 5
        volumeMounts:
        - name: uploads
          mountPath: /app/uploads
      volumes:
      - name: uploads
        persistentVolumeClaim:
          claimName: docn-uploads-pvc
---
apiVersion: v1
kind: Service
metadata:
  name: docn-service
spec:
  type: LoadBalancer
  selector:
    app: docn
  ports:
  - protocol: TCP
    port: 80
    targetPort: 8080
    name: http
  - protocol: TCP
    port: 443
    targetPort: 8081
    name: https
---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: docn-uploads-pvc
spec:
  accessModes:
    - ReadWriteMany
  storageClassName: azure-file
  resources:
    requests:
      storage: 100Gi
---
apiVersion: v1
kind: Secret
metadata:
  name: docn-secrets
type: Opaque
stringData:
  connection-string: "Server=sql-server;Database=DocN;..."
  gemini-api-key: "your-gemini-key"
  openai-api-key: "your-openai-key"
```

### Horizontal Pod Autoscaler

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: docn-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: docn-app
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
```

### Deploy su Kubernetes

```bash
# Apply secrets
kubectl apply -f secrets.yaml

# Deploy app
kubectl apply -f deployment.yaml

# Check status
kubectl get pods
kubectl get services

# View logs
kubectl logs -f deployment/docn-app

# Scale manually
kubectl scale deployment/docn-app --replicas=5
```

---

## ☁️ Deployment Azure App Service

### Azure CLI Deployment

```bash
# Login
az login

# Create resource group
az group create --name docn-rg --location eastus

# Create App Service Plan
az appservice plan create \
  --name docn-plan \
  --resource-group docn-rg \
  --sku P1V3 \
  --is-linux

# Create Web App
az webapp create \
  --name docn-app \
  --resource-group docn-rg \
  --plan docn-plan \
  --runtime "DOTNET:10.0"

# Create SQL Database
az sql server create \
  --name docn-sql \
  --resource-group docn-rg \
  --location eastus \
  --admin-user sqladmin \
  --admin-password "YOUR_SECURE_PASSWORD_HERE"

az sql db create \
  --name DocN \
  --resource-group docn-rg \
  --server docn-sql \
  --service-objective S2

# Create Storage Account
az storage account create \
  --name docnstorage \
  --resource-group docn-rg \
  --location eastus \
  --sku Standard_LRS

# Create Key Vault
az keyvault create \
  --name docn-kv \
  --resource-group docn-rg \
  --location eastus

# Store secrets
az keyvault secret set --vault-name docn-kv \
  --name "GeminiApiKey" --value "your-key"

# Deploy code
az webapp deployment source config-zip \
  --resource-group docn-rg \
  --name docn-app \
  --src docn-app.zip
```

### Azure DevOps Pipeline

```yaml
# azure-pipelines.yml
trigger:
  branches:
    include:
    - main

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'
  azureSubscription: 'Your-Subscription'
  webAppName: 'docn-app'

stages:
- stage: Build
  jobs:
  - job: BuildJob
    steps:
    - task: UseDotNet@2
      inputs:
        version: '10.x'
    
    - task: DotNetCoreCLI@2
      displayName: 'Restore'
      inputs:
        command: 'restore'
        projects: '**/*.csproj'
    
    - task: DotNetCoreCLI@2
      displayName: 'Build'
      inputs:
        command: 'build'
        projects: '**/*.csproj'
        arguments: '--configuration $(buildConfiguration)'
    
    - task: DotNetCoreCLI@2
      displayName: 'Test'
      inputs:
        command: 'test'
        projects: '**/*Tests.csproj'
    
    - task: DotNetCoreCLI@2
      displayName: 'Publish'
      inputs:
        command: 'publish'
        publishWebProjects: true
        arguments: '--configuration $(buildConfiguration) --output $(Build.ArtifactStagingDirectory)'
    
    - task: PublishBuildArtifacts@1
      inputs:
        PathtoPublish: '$(Build.ArtifactStagingDirectory)'
        ArtifactName: 'drop'

- stage: Deploy
  dependsOn: Build
  condition: succeeded()
  jobs:
  - deployment: DeployJob
    environment: 'production'
    strategy:
      runOnce:
        deploy:
          steps:
          - task: AzureWebApp@1
            inputs:
              azureSubscription: '$(azureSubscription)'
              appName: '$(webAppName)'
              package: '$(Pipeline.Workspace)/drop/**/*.zip'
```

---

## 🔧 Configurazione Post-Deployment

### 1. Database Setup

**Automatic Migrations (Recommended)**

Starting from version 1.1.0, DocN automatically applies pending database migrations on application startup. This includes the critical OwnerId foreign key fix.

When the application starts:
1. It checks for pending migrations
2. If found, applies them automatically
3. Logs the migration status
4. Continues startup even if migration fails (allows manual intervention)

```bash
# Simply start the application and check logs
dotnet run --project DocN.Server

# You should see in logs:
# "Applying X pending database migrations..."
# "Database migrations applied successfully"
```

**Manual Migration (Alternative)**

If you prefer manual control or automatic migration fails:

```bash
# Option 1: Using Entity Framework CLI
dotnet ef database update --project DocN.Data --startup-project DocN.Server

# Option 2: Using SQL script (for production environments)
sqlcmd -S your-server.database.windows.net -U sqladmin -P password \
  -d DocN -i Database/UpdateScripts/005_FixOwnerIdForeignKeyConstraint.sql

# Option 3: Using SQL Server Management Studio (SSMS)
# 1. Open SSMS and connect to your database
# 2. Open Database/UpdateScripts/005_FixOwnerIdForeignKeyConstraint.sql
# 3. Execute the script (F5)
```

**Verifying OwnerId Fix**

After migration, verify the fix was applied correctly:

```sql
-- Check that OwnerId is nullable
SELECT COLUMN_NAME, IS_NULLABLE, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Documents' AND COLUMN_NAME = 'OwnerId';
-- Expected: OwnerId | YES | nvarchar

-- Check the FK constraint
SELECT 
    fk.name AS ForeignKeyName,
    fk.delete_referential_action_desc AS DeleteAction
FROM sys.foreign_keys AS fk
WHERE fk.name LIKE '%OwnerId%' AND fk.parent_object_id = OBJECT_ID('Documents');
-- Expected: DeleteAction = SET_NULL
```

**Troubleshooting Database Save Errors**

If you encounter the error:
```
❌ ⚠️ ERRORE CRITICO: Il salvataggio nel database è fallito durante la creazione
```

This typically means the OwnerId migration hasn't been applied. Solutions:
1. Restart the application (automatic migration will run)
2. Manually apply migration using one of the methods above
3. Check the application logs for migration errors
4. Verify database user has permission to alter tables

See `Database/UpdateScripts/README_005_FixOwnerIdConstraint.md` for detailed information.

### 2. Configurazione Applicazione

```json
// appsettings.Production.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    },
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "Information"
      }
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "*** FROM KEY VAULT ***"
  },
  "FileStorage": {
    "Provider": "AzureBlobStorage",
    "AzureBlobStorage": {
      "ConnectionString": "*** FROM KEY VAULT ***",
      "ContainerName": "documents"
    }
  },
  "ApplicationInsights": {
    "ConnectionString": "*** FROM KEY VAULT ***"
  }
}
```

### 3. SSL/TLS Certificate

**Azure App Service:**
- Certificato gestito automaticamente
- Custom domain: aggiungi in Azure Portal

**IIS:**
```powershell
# Import certificate
Import-PfxCertificate -FilePath "certificate.pfx" `
  -CertStoreLocation Cert:\LocalMachine\My `
  -Password (ConvertTo-SecureString -String "password" -Force -AsPlainText)

# Bind to site
New-IISSiteBinding -Name "DocN" -Protocol https -Port 443 `
  -CertificateThumbPrint "THUMBPRINT"
```

### 4. Firewall Rules

```bash
# Azure SQL firewall
az sql server firewall-rule create \
  --resource-group docn-rg \
  --server docn-sql \
  --name AllowAppService \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0  # Allow Azure services

# Add specific IPs
az sql server firewall-rule create \
  --resource-group docn-rg \
  --server docn-sql \
  --name AllowOffice \
  --start-ip-address 1.2.3.4 \
  --end-ip-address 1.2.3.4
```

---

## 📊 Monitoring e Logging

### Application Insights (Azure)

```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
});

// Custom metrics
var telemetryClient = app.Services.GetRequiredService<TelemetryClient>();
telemetryClient.TrackEvent("DocumentUploaded", 
    new Dictionary<string, string> { { "UserId", userId } });
```

### Structured Logging (Serilog)

```csharp
// Program.cs
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/docn-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.ApplicationInsights(
        services.GetRequiredService<TelemetryConfiguration>(),
        TelemetryConverter.Traces));
```

---

## 🔄 CI/CD Pipeline

### GitHub Actions

```yaml
# .github/workflows/deploy.yml
name: Deploy to Azure

on:
  push:
    branches: [ main ]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '10.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --configuration Release --no-restore
    
    - name: Test
      run: dotnet test --no-build --verbosity normal
    
    - name: Publish
      run: dotnet publish DocN.Server/DocN.Server.csproj -c Release -o ./publish
    
    - name: Deploy to Azure
      uses: azure/webapps-deploy@v2
      with:
        app-name: 'docn-app'
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ./publish
```

---

## ✅ Post-Deployment Checklist

- [ ] Database migrations completate
- [ ] SSL certificate configurato
- [ ] Secrets in Key Vault
- [ ] Firewall rules configurate
- [ ] Health checks funzionanti (`/health`)
- [ ] Logging configurato
- [ ] Monitoring attivo
- [ ] Backup automatici configurati
- [ ] Disaster recovery testato
- [ ] Performance test eseguiti
- [ ] Security scan completato
- [ ] Documentazione aggiornata
- [ ] Team training completato

---

## 🚨 Troubleshooting

### App non parte

```bash
# Check logs
docker logs docn-app
kubectl logs deployment/docn-app
az webapp log tail --name docn-app --resource-group docn-rg

# Check environment variables
docker exec docn-app env
kubectl exec -it pod-name -- env
```

### Database connection failed

```bash
# Test connection
sqlcmd -S server -U user -P password -d DocN -Q "SELECT 1"

# Check firewall
az sql server firewall-rule list --resource-group docn-rg --server docn-sql
```

### Performance issues

```bash
# Check resource usage
docker stats
kubectl top pods
az monitor metrics list --resource /subscriptions/.../docn-app
```

---

**Versione:** 1.0  
**Data:** Dicembre 2024
