# 🚀 DocN - Stack Tecnologico Moderno Microsoft 2025

## 📦 Tecnologie Latest Release (Dicembre 2024)

### Framework Core
- ✅ **.NET 10.0** (Preview) - Ultima versione
- ✅ **Blazor Web App** - Server + WebAssembly hybrid
- ✅ **Entity Framework Core 10.0** - Con support VECTOR preview
- ✅ **ASP.NET Core Identity** - Authentication moderna

### Microsoft AI Stack
- ✅ **Microsoft Semantic Kernel 1.x** - Framework AI orchestration
- ✅ **Azure OpenAI SDK v2.x** - Client OpenAI ufficiale
- ✅ **Microsoft.Extensions.AI** - Abstractions AI unificate
- ✅ **Azure.AI.OpenAI** - Embeddings e Chat
- ✅ **Microsoft.SemanticKernel.Connectors.SqlServer** - Vector DB plugin

### SQL Server
- ✅ **SQL Server 2025 Preview** - VECTOR type nativo
- ✅ **Microsoft.Data.SqlClient 6.x** - Latest driver

### Caching & Performance
- ✅ **Redis 7.x** con **StackExchange.Redis**
- ✅ **System.Threading.Channels** - High-performance queues
- ✅ **Microsoft.Extensions.Caching.Memory** - L1 cache
- ✅ **Microsoft.Extensions.Caching.StackExchangeRedis** - L2 cache

### Monitoring & Observability
- ✅ **OpenTelemetry** - Tracing distribuito
- ✅ **Application Insights** - Monitoring Azure
- ✅ **Serilog** - Structured logging

### Testing
- ✅ **xUnit** - Unit testing
- ✅ **bUnit** - Blazor component testing
- ✅ **Testcontainers** - Integration testing con container

---

## 🎯 Microsoft Semantic Kernel - Implementazione

### Perché Semantic Kernel?
- 🧠 **Orchestrazione AI nativa** - Gestisce automaticamente prompt, memoria, planning
- 🔌 **Plugin system** - Estensibile con funzioni custom
- 💾 **Memory integrata** - Vector stores, chat history
- 🔄 **Function calling** - Auto-invoca funzioni quando necessario
- 📊 **Telemetry built-in** - Monitoring out-of-the-box

---

## 📁 Struttura Progetto Aggiornata

```
DocN/
├── DocN.Core/                          # Core domain logic
│   ├── Models/                         # Domain entities
│   ├── Interfaces/                     # Service contracts
│   └── ValueObjects/                   # DDD value objects
│
├── DocN.Infrastructure/                # Infrastructure layer
│   ├── Data/
│   │   ├── ApplicationDbContext.cs
│   │   ├── Repositories/
│   │   └── Migrations/
│   ├── AI/
│   │   ├── SemanticKernelService.cs   # SK orchestrator
│   │   ├── Plugins/                    # SK plugins
│   │   └── Memory/                     # Vector memory
│   └── External/
│       ├── AzureServices/
│       └── SqlServerVectorStore/
│
├── DocN.Application/                   # Application layer
│   ├── Services/                       # Application services
│   ├── DTOs/                          # Data transfer objects
│   ├── Queries/                       # CQRS queries
│   ├── Commands/                      # CQRS commands
│   └── Validators/                    # FluentValidation
│
├── DocN.Web/                          # Blazor Web UI
│   ├── Components/
│   │   ├── Pages/
│   │   ├── Layout/
│   │   └── Shared/
│   ├── wwwroot/
│   └── Program.cs
│
└── DocN.Tests/
    ├── Unit/
    ├── Integration/
    └── E2E/
```

---

## 🔧 Installazione Packages Latest

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <!-- Microsoft Semantic Kernel - Latest -->
    <PackageReference Include="Microsoft.SemanticKernel" Version="1.4.0" />
    <PackageReference Include="Microsoft.SemanticKernel.Connectors.OpenAI" Version="1.4.0" />
    <PackageReference Include="Microsoft.SemanticKernel.Connectors.SqlServer" Version="1.4.0-preview" />
    <PackageReference Include="Microsoft.SemanticKernel.Plugins.Memory" Version="1.4.0-alpha" />
    
    <!-- Microsoft AI Abstractions -->
    <PackageReference Include="Microsoft.Extensions.AI" Version="9.0.0-preview.1" />
    <PackageReference Include="Microsoft.Extensions.AI.OpenAI" Version="9.0.0-preview.1" />
    
    <!-- Azure OpenAI -->
    <PackageReference Include="Azure.AI.OpenAI" Version="2.0.0-beta.5" />
    
    <!-- Entity Framework Core 10 -->
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.0-preview.1" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.0-preview.1" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="10.0.0-preview.1" />
    
    <!-- Caching -->
    <PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="10.0.0-preview.1" />
    
    <!-- Logging & Telemetry -->
    <PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
    <PackageReference Include="Serilog.Sinks.Console" Version="5.0.1" />
    <PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
    <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.7.0" />
    <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.7.0" />
    <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.7.0" />
    
    <!-- Utilities -->
    <PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />
    <PackageReference Include="Humanizer.Core" Version="2.14.1" />
  </ItemGroup>
</Project>
```

Ora implemento i servizi modernizzati!
