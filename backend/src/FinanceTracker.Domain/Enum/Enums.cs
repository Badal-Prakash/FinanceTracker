namespace FinanceTracker.Domain.Enums;

public enum ExpenseStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3
}

public enum InvoiceStatus
{
    Unpaid = 0,
    Paid = 1,
    Overdue = 2,
    Cancelled = 3
}

public enum UserRole
{
    Employee = 0,
    Manager = 1,
    Admin = 2,
    SuperAdmin = 3
}

public enum TenantPlan
{
    Free = 0,
    Pro = 1,
    Enterprise = 2
}