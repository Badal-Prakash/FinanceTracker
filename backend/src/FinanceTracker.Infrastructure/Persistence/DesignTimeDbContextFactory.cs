using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;


namespace FinanceTracker.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Build config from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(),
                "../FinanceTracker.API"))
            .AddJsonFile("appsettings.json")
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));

        // Pass a dummy CurrentUserService for design time
        return new ApplicationDbContext(optionsBuilder.Options, new DesignTimeCurrentUserService());
    }
}

// Dummy service just for migrations — returns empty values
public class DesignTimeCurrentUserService : Application.Common.Interfaces.ICurrentUserService
{
    public Guid UserId => Guid.Empty;
    public Guid TenantId => Guid.Empty;
    public string Email => string.Empty;
    public string Role => string.Empty;
    public bool IsAuthenticated => false;
}