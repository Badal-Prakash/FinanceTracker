using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Enums;

namespace FinanceTracker.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Subdomain { get; private set; } = string.Empty;
    public TenantPlan Plan { get; private set; } = TenantPlan.Free;
    public bool IsActive { get; private set; } = true;

    private Tenant() { }

    public static Tenant Create(string name, string subdomain, TenantPlan plan = TenantPlan.Free)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        if (string.IsNullOrWhiteSpace(subdomain))
            throw new ArgumentException("Subdomain cannot be empty.", nameof(subdomain));

        return new Tenant
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = name,
            Subdomain = subdomain.ToLower().Trim(),
            Plan = plan,
            IsActive = true
        };
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
    public void ChangePlan(TenantPlan newPlan) => Plan = newPlan;
}