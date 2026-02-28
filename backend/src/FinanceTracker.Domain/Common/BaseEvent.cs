using System.ComponentModel.DataAnnotations.Schema;
using MediatR;

namespace FinanceTracker.Domain.Common;

public abstract class BaseEvent : INotification
{
    [NotMapped]
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}