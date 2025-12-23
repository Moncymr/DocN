using DocN.Core.Extensions;
using DocN.Core.AI.Interfaces;
using DocN.Core.AI.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Registra i servizi AI DocN
builder.Services.AddDocNAIServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// Endpoint di esempio per dimostrare l'uso dei provider AI
app.MapGet("/ai/providers", (IAIProviderFactory aiFactory) =>
{
    var providers = new[]
    {
        aiFactory.CreateProvider(AIProviderType.AzureOpenAI),
        aiFactory.CreateProvider(AIProviderType.OpenAI),
        aiFactory.CreateProvider(AIProviderType.Gemini)
    };

    return new
    {
        DefaultProvider = aiFactory.GetDefaultProvider().ProviderName,
        AvailableProviders = providers.Select(p => new
        {
            Type = p.ProviderType.ToString(),
            Name = p.ProviderName
        })
    };
})
.WithName("GetAIProviders")
.WithDescription("Restituisce i provider AI disponibili e quello predefinito");

// Endpoint di esempio per analisi documento (richiede configurazione valida)
app.MapPost("/ai/analyze", async (DocumentRequest request, IAIProviderFactory aiFactory) =>
{
    try
    {
        var provider = string.IsNullOrEmpty(request.ProviderType)
            ? aiFactory.GetDefaultProvider()
            : aiFactory.CreateProvider(Enum.Parse<AIProviderType>(request.ProviderType));

        var result = await provider.AnalyzeDocumentAsync(
            request.DocumentText,
            request.AvailableCategories);

        return Results.Ok(new
        {
            Provider = provider.ProviderName,
            Result = result
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new
        {
            Error = ex.Message,
            Message = "Assicurati di aver configurato correttamente il provider nel file appsettings.json"
        });
    }
})
.WithName("AnalyzeDocument")
.WithDescription("Analizza un documento usando un provider AI specificato o quello predefinito");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

record DocumentRequest(string DocumentText, List<string> AvailableCategories, string? ProviderType = null);
