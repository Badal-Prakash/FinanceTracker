using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FinanceTracker.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(),
                "../FinanceTracker.API"))
            .AddJsonFile("appsettings.json")
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));

        return new ApplicationDbContext(
            optionsBuilder.Options,
            new DesignTimeCurrentUserService(),
            new DesignTimePublisher());
    }
}

// Dummy CurrentUserService for migrations — returns empty values
public class DesignTimeCurrentUserService
    : Application.Common.Interfaces.ICurrentUserService
{
    public Guid UserId => Guid.Empty;
    public Guid TenantId => Guid.Empty;
    public string Email => string.Empty;
    public string Role => string.Empty;
    public bool IsAuthenticated => false;
}

// Dummy IPublisher for migrations — does nothing
public class DesignTimePublisher : IPublisher
{
    public Task Publish(object notification,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task Publish<TNotification>(TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification => Task.CompletedTask;
}