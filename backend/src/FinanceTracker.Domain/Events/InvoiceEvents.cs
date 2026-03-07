using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Domain.Events;

public class InvoicePaidEvent : BaseEvent
{
    public Invoice Invoice { get; }
    public InvoicePaidEvent(Invoice invoice) => Invoice = invoice;
}

public class InvoiceOverdueEvent : BaseEvent
{
    public Invoice Invoice { get; }
    public InvoiceOverdueEvent(Invoice invoice) => Invoice = invoice;
}