using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DocN.Data.Models;

namespace DocN.Data.Services;

/// <summary>
/// Seeds initial data for the application including tenants and default user
/// </summary>
public class ApplicationSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<ApplicationSeeder> _logger;

    public ApplicationSeeder(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<ApplicationSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            // Note: MigrateAsync is commented out because EF Core doesn't support VECTOR type yet
            // Use the SQL script Database/CreateDatabase_Complete_V2.sql to create the database
            // await _context.Database.MigrateAsync();
            
            // Seed default tenant
            var defaultTenant = await SeedDefaultTenantAsync();
            
            // Seed roles
            await SeedRolesAsync();
            
            // Seed default user
            await SeedDefaultUserAsync(defaultTenant);
            
            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private async Task<Tenant> SeedDefaultTenantAsync()
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Name == "Default");
        
        if (tenant == null)
        {
            tenant = new Tenant
            {
                Name = "Default",
                Description = "Default tenant created automatically on first run",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Created default tenant");
        }
        else
        {
            _logger.LogInformation("Default tenant already exists");
        }
        
        return tenant;
    }

    private async Task SeedRolesAsync()
    {
        var roles = new[] { "Admin", "User", "Manager" };
        
        foreach (var roleName in roles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
                if (result.Succeeded)
                {
                    _logger.LogInformation("Created role: {Role}", roleName);
                }
                else
                {
                    _logger.LogWarning("Failed to create role {Role}: {Errors}", 
                        roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    private async Task SeedDefaultUserAsync(Tenant tenant)
    {
        const string defaultEmail = "admin@docn.local";
        const string defaultPassword = "Admin@123";
        
        var existingUser = await _userManager.FindByEmailAsync(defaultEmail);
        
        if (existingUser == null)
        {
            var user = new ApplicationUser
            {
                UserName = defaultEmail,
                Email = defaultEmail,
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "User",
                IsActive = true,
                TenantId = tenant.Id,
                CreatedAt = DateTime.UtcNow
            };
            
            var result = await _userManager.CreateAsync(user, defaultPassword);
            
            if (result.Succeeded)
            {
                // Assign Admin role
                await _userManager.AddToRoleAsync(user, "Admin");
                
                _logger.LogInformation("Created default admin user: {Email}", defaultEmail);
                _logger.LogWarning("⚠️  IMPORTANT: Change the default admin password after first login!");
            }
            else
            {
                _logger.LogError("Failed to create default user: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            _logger.LogInformation("Default admin user already exists");
            
            // Ensure the user is connected to the default tenant
            if (existingUser.TenantId == null)
            {
                existingUser.TenantId = tenant.Id;
                await _userManager.UpdateAsync(existingUser);
                _logger.LogInformation("Connected existing admin user to default tenant");
            }
        }
    }
}
