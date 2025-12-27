using DocN.Client.Components;
using DocN.Data;
using DocN.Data.Models;
using DocN.Data.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add HttpClient for Blazor components
builder.Services.AddHttpClient();

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Use a fallback connection string only in development
if (string.IsNullOrEmpty(connectionString))
{
    if (builder.Environment.IsDevelopment())
    {
        connectionString = "Server=NTSPJ-060-02\\SQL2025;Database=DocumentArchive;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=True";
    }
    else
    {
        throw new InvalidOperationException("Database connection string 'DefaultConnection' is not configured. Please set it in appsettings.json or environment variables.");
    }
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlServerOptions =>
    {
        // Enable retry on transient failures
        sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });

    // Enable sensitive data logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Identity & Authentication
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

// Application Settings
builder.Services.Configure<FileStorageSettings>(builder.Configuration.GetSection("FileStorage"));
builder.Services.Configure<AISettings>(builder.Configuration.GetSection("AI"));
builder.Services.Configure<OpenAISettings>(builder.Configuration.GetSection("OpenAI"));
builder.Services.Configure<GeminiSettings>(builder.Configuration.GetSection("Gemini"));
builder.Services.Configure<EmbeddingsSettings>(builder.Configuration.GetSection("Embeddings"));

// Application Services
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<DocN.Data.Services.IEmbeddingService, EmbeddingService>();
builder.Services.AddScoped<DocN.Data.Services.ICategoryService, CategoryService>();
builder.Services.AddScoped<IDocumentStatisticsService, DocumentStatisticsService>();
builder.Services.AddScoped<IMultiProviderAIService, MultiProviderAIService>();
builder.Services.AddScoped<IFileProcessingService, FileProcessingService>();

// Register ApplicationSeeder
builder.Services.AddScoped<DocN.Data.Services.ApplicationSeeder>();

// Configure HttpClient to call the backend API
builder.Services.AddHttpClient("BackendAPI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["BackendApiUrl"] ?? "https://localhost:5001/");
});

var app = builder.Build();

// Seed the database with default tenant and user
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DocN.Data.Services.ApplicationSeeder>();
    await seeder.SeedAsync();
}

// Ensure upload directory exists
var fileStorageSettings = builder.Configuration.GetSection("FileStorage").Get<FileStorageSettings>();
if (fileStorageSettings != null && !string.IsNullOrEmpty(fileStorageSettings.UploadPath))
{
    // Ensure the path is safe and create directory
    var uploadPath = Path.GetFullPath(fileStorageSettings.UploadPath);
    Directory.CreateDirectory(uploadPath);
}

// Configure the HTTP request pipeline.
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

// Authentication endpoints
app.MapPost("/logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/");
}).RequireAuthorization();

app.MapPost("/account/login", async (
    HttpContext context,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) =>
{
    var form = await context.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();
    var rememberMe = form["rememberMe"].ToString() == "true";

    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
    {
        return Results.Redirect("/login?error=invalid");
    }

    var user = await userManager.FindByEmailAsync(email);
    if (user == null)
    {
        return Results.Redirect("/login?error=invalid");
    }

    var result = await signInManager.PasswordSignInAsync(
        user.UserName!,
        password,
        rememberMe,
        lockoutOnFailure: true
    );

    if (result.Succeeded)
    {
        // Update last login time
        user.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);
        return Results.Redirect("/");
    }
    else if (result.IsLockedOut)
    {
        return Results.Redirect("/login?error=locked");
    }
    else if (result.RequiresTwoFactor)
    {
        return Results.Redirect("/login?error=2fa");
    }
    else
    {
        return Results.Redirect("/login?error=invalid");
    }
}).AllowAnonymous();

app.MapPost("/account/register", async (
    HttpContext context,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) =>
{
    var form = await context.Request.ReadFormAsync();
    var firstName = form["firstName"].ToString();
    var lastName = form["lastName"].ToString();
    var email = form["email"].ToString();
    var password = form["password"].ToString();
    var confirmPassword = form["confirmPassword"].ToString();

    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || 
        string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
    {
        return Results.Redirect("/register?error=required");
    }

    if (password != confirmPassword)
    {
        return Results.Redirect("/register?error=mismatch");
    }

    // Check if user already exists
    var existingUser = await userManager.FindByEmailAsync(email);
    if (existingUser != null)
    {
        return Results.Redirect("/register?error=exists");
    }

    var user = new ApplicationUser
    {
        UserName = email,
        Email = email,
        FirstName = firstName,
        LastName = lastName,
        CreatedAt = DateTime.UtcNow,
        IsActive = true
    };

    var result = await userManager.CreateAsync(user, password);

    if (result.Succeeded)
    {
        // Sign in the user
        await signInManager.SignInAsync(user, isPersistent: false);
        return Results.Redirect("/?registered=true");
    }
    else
    {
        var error = result.Errors.FirstOrDefault()?.Code ?? "unknown";
        return Results.Redirect($"/register?error={error}");
    }
}).AllowAnonymous();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
