using DocN.Client.Components;
using DocN.Data;
using DocN.Data.Models;
using DocN.Data.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Serilog;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// ============================================================================
// CONFIGURAZIONE SERILOG - Logging strutturato
// ============================================================================
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/docn-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("🚀 Avvio DocN - Sistema RAG Aziendale");

    var builder = WebApplication.CreateBuilder(args);

    // Usa Serilog come provider di logging
    builder.Host.UseSerilog();

    // ========================================================================
    // SERVIZI CORE
    // ========================================================================

    // Blazor components
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // ========================================================================
    // DATABASE CONFIGURATION
    // ========================================================================
    
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Server=(localdb)\\mssqllocaldb;Database=DocNDb;Trusted_Connection=True;MultipleActiveResultSets=true";
    
    Log.Information("Configurazione database: {ConnectionString}", 
        connectionString.Replace(connectionString.Split(';')[0], "Server=***"));

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseSqlServer(connectionString);
        
        // Enable sensitive data logging in development
        if (builder.Environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        }
    });

    // ========================================================================
    // IDENTITY & AUTHENTICATION
    // ========================================================================
    
    Log.Information("Configurazione Identity e Authentication...");
    
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Password policy configuration
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.User.RequireUniqueEmail = true;
        
        // Lockout configuration
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    builder.Services.AddCascadingAuthenticationState();

    // ========================================================================
    // MICROSOFT SEMANTIC KERNEL - AI Orchestration
    // ========================================================================
    
    Log.Information("⚡ Configurazione Microsoft Semantic Kernel...");

    // Recupera configurazione Azure OpenAI
    var azureOpenAIEndpoint = builder.Configuration["AzureOpenAI:Endpoint"];
    var azureOpenAIKey = builder.Configuration["AzureOpenAI:ApiKey"];
    var chatDeploymentName = builder.Configuration["AzureOpenAI:ChatDeploymentName"] ?? "gpt-4";
    var embeddingDeploymentName = builder.Configuration["AzureOpenAI:EmbeddingDeploymentName"] ?? "text-embedding-ada-002";

    // Registra Semantic Kernel come singleton
    builder.Services.AddSingleton<Kernel>(sp =>
    {
        var kernelBuilder = Kernel.CreateBuilder();

        // Configura Azure OpenAI per chat completion
        if (!string.IsNullOrEmpty(azureOpenAIEndpoint) && !string.IsNullOrEmpty(azureOpenAIKey))
        {
            Log.Information("Configurazione Azure OpenAI - Chat: {Deployment}", chatDeploymentName);
            
            kernelBuilder.AddAzureOpenAIChatCompletion(
                deploymentName: chatDeploymentName,
                endpoint: azureOpenAIEndpoint,
                apiKey: azureOpenAIKey);

            Log.Information("Configurazione Azure OpenAI - Embeddings: {Deployment}", embeddingDeploymentName);
            
            // Configura embeddings service
            kernelBuilder.AddAzureOpenAITextEmbeddingGeneration(
                deploymentName: embeddingDeploymentName,
                endpoint: azureOpenAIEndpoint,
                apiKey: azureOpenAIKey);
        }
        else
        {
            Log.Warning("⚠️ Azure OpenAI non configurato. Alcune funzionalità AI non saranno disponibili.");
        }

        // Aggiungi logging per Semantic Kernel
        var logger = sp.GetRequiredService<ILoggerFactory>();
        kernelBuilder.Services.AddLogging(c => c.AddSerilog());

        return kernelBuilder.Build();
    });

    // Registra Semantic Memory per vector search
    builder.Services.AddSingleton<ISemanticTextMemory>(sp =>
    {
        var kernel = sp.GetRequiredService<Kernel>();
        
        // Crea memory store con SQL Server come backend
        // Per ora usiamo VolatileMemoryStore (in-memory)
        // TODO: Usare SqlServerMemoryStore quando disponibile
        var memoryBuilder = new MemoryBuilder();
        
        memoryBuilder.WithMemoryStore(new VolatileMemoryStore());
        
        // Usa il text embedding service di Kernel
        memoryBuilder.WithTextEmbeddingGeneration(
            kernel.GetRequiredService<ITextEmbeddingGenerationService>());

        Log.Information("✅ Semantic Memory configurata con VolatileMemoryStore");
        
        return memoryBuilder.Build();
    });

    // ========================================================================
    // CACHING - Multi-Level (Memory + Redis)
    // ========================================================================
    
    Log.Information("💾 Configurazione Caching...");

    // L1 Cache: In-Memory
    builder.Services.AddMemoryCache();

    // L2 Cache: Redis (se configurato)
    var redisConnection = builder.Configuration.GetConnectionString("Redis");
    if (!string.IsNullOrEmpty(redisConnection))
    {
        Log.Information("Configurazione Redis: {Connection}", redisConnection);
        
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = "DocN_";
        });
    }
    else
    {
        Log.Information("Redis non configurato, uso solo cache in-memory");
        builder.Services.AddDistributedMemoryCache();
    }

    // ========================================================================
    // APPLICATION SETTINGS
    // ========================================================================
    
    builder.Services.Configure<FileStorageSettings>(builder.Configuration.GetSection("FileStorage"));
    builder.Services.Configure<AISettings>(builder.Configuration.GetSection("AI"));
    builder.Services.Configure<OpenAISettings>(builder.Configuration.GetSection("OpenAI"));
    builder.Services.Configure<GeminiSettings>(builder.Configuration.GetSection("Gemini"));
    builder.Services.Configure<EmbeddingsSettings>(builder.Configuration.GetSection("Embeddings"));

    // ========================================================================
    // APPLICATION SERVICES
    // ========================================================================
    
    Log.Information("📦 Registrazione servizi applicativi...");

    // Servizi legacy (da migrare)
    builder.Services.AddScoped<IDocumentService, DocumentService>();
    builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
    builder.Services.AddScoped<ICategoryService, CategoryService>();
    builder.Services.AddScoped<IDocumentStatisticsService, DocumentStatisticsService>();
    builder.Services.AddScoped<IMultiProviderAIService, MultiProviderAIService>();

    // Servizi moderni con Semantic Kernel
    builder.Services.AddScoped<IModernRAGService, ModernRAGService>();
    
    // TODO: Altri servizi da aggiungere
    // builder.Services.AddScoped<ISmartSearchService, SmartSearchService>();
    // builder.Services.AddScoped<IDocumentChunkingService, DocumentChunkingService>();
    // builder.Services.AddScoped<IConversationService, ConversationService>();

    // ========================================================================
    // OPENTELEMETRY - Distributed Tracing
    // ========================================================================
    
    if (builder.Configuration.GetValue<bool>("OpenTelemetry:Enabled"))
    {
        Log.Information("📊 Configurazione OpenTelemetry...");
        
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService("DocN"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:Endpoint"] 
                        ?? "http://localhost:4317");
                }));
    }

    // ========================================================================
    // CONFIGURAZIONE DIRECTORY
    // ========================================================================
    
    // Assicura che la directory di upload esista
    var fileStorageSettings = builder.Configuration.GetSection("FileStorage").Get<FileStorageSettings>();
    if (fileStorageSettings != null && !string.IsNullOrEmpty(fileStorageSettings.UploadPath))
    {
        Directory.CreateDirectory(fileStorageSettings.UploadPath);
        Log.Information("📁 Directory upload: {Path}", fileStorageSettings.UploadPath);
    }

    // ========================================================================
    // BUILD APPLICATION
    // ========================================================================
    
    var app = builder.Build();

    Log.Information("🔧 Inizializzazione applicazione...");

    // ========================================================================
    // DATABASE INITIALIZATION
    // ========================================================================
    
    // Applica migrazioni automaticamente all'avvio
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        
        try
        {
            Log.Information("🗄️ Controllo e applicazione migrazioni database...");
            
            var context = services.GetRequiredService<ApplicationDbContext>();
            
            // Verifica connessione
            if (await context.Database.CanConnectAsync())
            {
                Log.Information("✅ Connessione database stabilita");
                
                // Applica migrazioni pendenti
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    Log.Information("📝 Applicazione {Count} migrazioni pendenti...", pendingMigrations.Count());
                    await context.Database.MigrateAsync();
                    Log.Information("✅ Migrazioni applicate con successo");
                }
                else
                {
                    Log.Information("✅ Database già aggiornato");
                }
            }
            else
            {
                Log.Warning("⚠️ Impossibile connettersi al database");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Errore durante l'inizializzazione del database");
        }
    }

    // ========================================================================
    // HTTP PIPELINE CONFIGURATION
    // ========================================================================
    
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseAntiforgery();
    
    // Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // ========================================================================
    // ENDPOINTS
    // ========================================================================
    
    // Logout endpoint
    app.MapPost("/logout", async (SignInManager<ApplicationUser> signInManager, HttpContext context) =>
    {
        await signInManager.SignOutAsync();
        return Results.Redirect("/");
    }).RequireAuthorization();

    // Map Razor components
    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    // ========================================================================
    // START APPLICATION
    // ========================================================================
    
    Log.Information("✨ DocN avviato con successo!");
    Log.Information("🌐 URL: {Urls}", string.Join(", ", app.Urls));
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Errore fatale durante l'avvio dell'applicazione");
    throw;
}
finally
{
    Log.Information("🛑 DocN terminato");
    Log.CloseAndFlush();
}
