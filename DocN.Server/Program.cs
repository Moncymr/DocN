using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using DocN.Data;
using DocN.Data.Services;
using DocN.Data.Services.Agents;
using DocN.Server.Services;
using DocN.Server.Services.HealthChecks;
using DocN.Server.Middleware;
using DocN.Core.Interfaces;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only
#pragma warning disable SKEXP0010 // Method is for evaluation purposes only
#pragma warning disable SKEXP0110 // Agents are experimental

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "DocN API",
        Version = "v1",
        Description = "API REST per la gestione di documenti con RAG (Retrieval-Augmented Generation) e ricerca semantica",
        Contact = new Microsoft.OpenApi.OpenApiContact
        {
            Name = "DocN Support",
            Email = "api-support@docn.example.com"
        }
    });

    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Add memory cache for caching service
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024 * 1024 * 100; // 100MB cache limit
});

// Add HttpContextAccessor for audit logging
builder.Services.AddHttpContextAccessor();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5210", "https://localhost:5211", "http://localhost:5036", "https://localhost:7114")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Rate Limiting for API protection
builder.Services.AddRateLimiter(options =>
{
    // Fixed window limiter for general API endpoints
    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });
    
    // Sliding window for document uploads
    options.AddSlidingWindowLimiter("upload", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(15);
        opt.PermitLimit = 20;
        opt.SegmentsPerWindow = 3;
    });
    
    // Concurrency limiter for AI operations
    options.AddConcurrencyLimiter("ai", opt =>
    {
        opt.PermitLimit = 20;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 50;
    });
    
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        
        double? retryAfterSeconds = null;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            retryAfterSeconds = retryAfter.TotalSeconds;
        }
        
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Too many requests. Please try again later.",
            retryAfter = retryAfterSeconds
        }, cancellationToken: token);
    };
});

// Configure DbContext
builder.Services.AddDbContext<DocArcContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DocArc");
    if (!string.IsNullOrEmpty(connectionString))
    {
        options.UseSqlServer(connectionString);
    }
    else
    {
        // Use in-memory database for development if no connection string is provided
        options.UseInMemoryDatabase("DocArc");
    }
});

// Also register ApplicationDbContext for the new services
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                        ?? builder.Configuration.GetConnectionString("DocArc");
    if (!string.IsNullOrEmpty(connectionString))
    {
        options.UseSqlServer(connectionString);
    }
    else
    {
        options.UseInMemoryDatabase("DocArc");
    }
});

// Configure Semantic Kernel
var kernelBuilder = Kernel.CreateBuilder();

// Configure AI services based on configuration
var azureOpenAIEndpoint = builder.Configuration["AzureOpenAI:Endpoint"];
var azureOpenAIKey = builder.Configuration["AzureOpenAI:ApiKey"];
var azureOpenAIChatDeployment = builder.Configuration["AzureOpenAI:ChatDeployment"] ?? "gpt-4";
var azureOpenAIEmbeddingDeployment = builder.Configuration["AzureOpenAI:EmbeddingDeployment"] ?? "text-embedding-ada-002";

// Track whether AI services are configured
bool hasAIServicesConfigured = false;

if (!string.IsNullOrEmpty(azureOpenAIEndpoint) && !string.IsNullOrEmpty(azureOpenAIKey))
{
    // Add Azure OpenAI Chat Completion
    kernelBuilder.AddAzureOpenAIChatCompletion(
        deploymentName: azureOpenAIChatDeployment,
        endpoint: azureOpenAIEndpoint,
        apiKey: azureOpenAIKey);

    // Add Azure OpenAI Text Embedding
    kernelBuilder.AddAzureOpenAITextEmbeddingGeneration(
        deploymentName: azureOpenAIEmbeddingDeployment,
        endpoint: azureOpenAIEndpoint,
        apiKey: azureOpenAIKey);
    
    hasAIServicesConfigured = true;
}
else
{
    // Fallback to OpenAI if Azure is not configured
    var openAIKey = builder.Configuration["OpenAI:ApiKey"];
    if (!string.IsNullOrEmpty(openAIKey))
    {
        kernelBuilder.AddOpenAIChatCompletion(
            modelId: "gpt-4",
            apiKey: openAIKey);

        kernelBuilder.AddOpenAITextEmbeddingGeneration(
            modelId: "text-embedding-ada-002",
            apiKey: openAIKey);
        
        hasAIServicesConfigured = true;
    }
}

var kernel = kernelBuilder.Build();
builder.Services.AddSingleton(kernel);

// Register core services - use Data layer implementations
builder.Services.AddScoped<DocN.Data.Services.IEmbeddingService, DocN.Data.Services.EmbeddingService>();
builder.Services.AddScoped<DocN.Data.Services.IChunkingService, DocN.Data.Services.ChunkingService>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IHybridSearchService, HybridSearchService>();
builder.Services.AddScoped<IBatchProcessingService, BatchProcessingService>();
builder.Services.AddScoped<ILogService, LogService>();

// Register Audit Service for GDPR/SOC2 compliance
builder.Services.AddScoped<IAuditService, AuditService>();

// Register Semantic RAG Service (new advanced RAG with Semantic Kernel)
// Use NoOpSemanticRAGService if AI services are not configured in Kernel (e.g., when using Gemini from DB config)
if (hasAIServicesConfigured)
{
    builder.Services.AddScoped<ISemanticRAGService, SemanticRAGService>();
    
    // Register agents only when AI services are available
    builder.Services.AddScoped<IRetrievalAgent, RetrievalAgent>();
    builder.Services.AddScoped<ISynthesisAgent, SynthesisAgent>();
    builder.Services.AddScoped<IClassificationAgent, ClassificationAgent>();
    builder.Services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();
}
else
{
    builder.Services.AddScoped<ISemanticRAGService, NoOpSemanticRAGService>();
}

// Register Agent Configuration services
builder.Services.AddScoped<IAgentConfigurationService, AgentConfigurationService>();
builder.Services.AddScoped<AgentTemplateSeeder>();

// Register background services
builder.Services.AddHostedService<BatchEmbeddingProcessor>();

// Register DatabaseSeeder
builder.Services.AddScoped<DatabaseSeeder>();

// Add Health Checks for monitoring and orchestration
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database", tags: new[] { "ready", "db" })
    .AddCheck<AIProviderHealthCheck>("ai_provider", tags: new[] { "ready", "ai" })
    .AddCheck<OCRServiceHealthCheck>("ocr_service", tags: new[] { "ready", "ocr" })
    .AddCheck<SemanticKernelHealthCheck>("semantic_kernel", tags: new[] { "ready", "orchestration" });

var app = builder.Build();

// Apply pending migrations automatically
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        
        if (pendingMigrations.Any())
        {
            logger.LogInformation("Applying {Count} pending database migrations...", pendingMigrations.Count());
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully");
        }
        else
        {
            logger.LogInformation("Database is up to date - no pending migrations");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying database migrations");
        // Continue application startup even if migration fails
        // This allows manual migration via SQL scripts if needed
    }
}

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
    
    // Seed agent templates
    var agentTemplateSeeder = scope.ServiceProvider.GetRequiredService<AgentTemplateSeeder>();
    await agentTemplateSeeder.SeedTemplatesAsync();
}

// Configure the HTTP request pipeline.
// Enable Swagger in all environments for API documentation
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "DocN API v1");
    options.RoutePrefix = "swagger"; // Swagger UI at /swagger
    options.DocumentTitle = "DocN API Documentation";
    options.DisplayRequestDuration();
});

// Add Security Headers Middleware
app.UseMiddleware<SecurityHeadersMiddleware>();

// Add Rate Limiting
app.UseRateLimiter();

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Add comprehensive health check endpoints
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        });
        await context.Response.WriteAsync(result);
    }
});

// Liveness probe - just checks if the app is running
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // No checks, just returns healthy if app is running
});

// Readiness probe - checks if app is ready to serve requests
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
