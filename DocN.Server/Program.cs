using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using DocN.Data;
using DocN.Data.Services;
using DocN.Data.Services.Agents;
using DocN.Server.Services;
using DocN.Core.Interfaces;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only
#pragma warning disable SKEXP0010 // Method is for evaluation purposes only
#pragma warning disable SKEXP0110 // Agents are experimental

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add memory cache for caching service
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024 * 1024 * 100; // 100MB cache limit
});

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5210", "https://localhost:5211")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
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

// Register Semantic RAG Service (new advanced RAG with Semantic Kernel)
builder.Services.AddScoped<ISemanticRAGService, SemanticRAGService>();

// Register agents
builder.Services.AddScoped<IRetrievalAgent, RetrievalAgent>();
builder.Services.AddScoped<ISynthesisAgent, SynthesisAgent>();
builder.Services.AddScoped<IClassificationAgent, ClassificationAgent>();
builder.Services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();

// Register background services
builder.Services.AddHostedService<BatchEmbeddingProcessor>();

// Register DatabaseSeeder
builder.Services.AddScoped<DatabaseSeeder>();

var app = builder.Build();

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
