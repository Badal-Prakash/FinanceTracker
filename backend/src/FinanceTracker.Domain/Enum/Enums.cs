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
    Draft = 0,
    Unpaid = 1,
    Paid = 2,
    Overdue = 3,
    Cancelled = 4
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