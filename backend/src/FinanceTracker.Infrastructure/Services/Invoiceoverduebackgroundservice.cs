using FinanceTracker.Domain.Enums;
using FinanceTracker.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinanceTracker.Infrastructure.Services;

/// <summary>
/// Runs once at startup and then every hour.
/// Finds all Unpaid invoices whose DueDate has passed and marks them Overdue,
/// which fires InvoiceOverdueEvent → notification handlers automatically.
/// </summary>
public class InvoiceOverdueBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InvoiceOverdueBackgroundService> _logger;

    // How often to check — every hour is fine for day-resolution due dates
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    public InvoiceOverdueBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<InvoiceOverdueBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "InvoiceOverdueBackgroundService started. Running every {Interval}.", Interval);

        // Run immediately on startup, then on interval
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        _logger.LogDebug("Checking for overdue invoices...");

        // BackgroundService is a singleton — always create a new scope
        // to get scoped services (DbContext, IPublisher)
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        try
        {
            var now = DateTime.UtcNow;

            // Load all Unpaid invoices past their due date across ALL tenants
            // (background job is tenant-agnostic — bypass the query filter)
            var overdueInvoices = await db.Invoices
                .IgnoreQueryFilters()
                .Where(i => i.Status == InvoiceStatus.Unpaid && i.DueDate < now)
                .ToListAsync(ct);

            if (overdueInvoices.Count == 0)
            {
                _logger.LogDebug("No overdue invoices found.");
                return;
            }

            _logger.LogInformation(
                "Found {Count} invoice(s) to mark as overdue.", overdueInvoices.Count);

            // Collect domain events — MarkAsOverdue() adds InvoiceOverdueEvent
            foreach (var invoice in overdueInvoices)
                invoice.MarkAsOverdue();

            // SaveChangesAsync in ApplicationDbContext dispatches domain events
            // automatically, which triggers InvoiceOverdueNotificationHandler
            var saved = await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Marked {Count} invoice(s) as overdue. DB rows affected: {Saved}.",
                overdueInvoices.Count, saved);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown — don't log as error
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "An error occurred while processing overdue invoices.");
        }
    }
}