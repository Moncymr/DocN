using Microsoft.EntityFrameworkCore;
using DocN.Data;
using DocN.Data.Services;
using DocN.Data.Services.Agents;
using DocN.Server.Services;

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
        options.UseSqlServer(connectionString, sqlServerOptions =>
        {
            sqlServerOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });
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
        options.UseSqlServer(connectionString, sqlServerOptions =>
        {
            sqlServerOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });
    }
    else
    {
        options.UseInMemoryDatabase("DocArc");
    }
});

// Register core services
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddScoped<IChunkingService, ChunkingService>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IHybridSearchService, HybridSearchService>();
builder.Services.AddScoped<IBatchProcessingService, BatchProcessingService>();

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
