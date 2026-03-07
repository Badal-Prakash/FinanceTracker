using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Application.Team;

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public record TeamMemberDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    // Stats
    int TotalExpenses,
    decimal TotalExpenseAmount,
    int PendingExpenses);

public record TeamStatsDto(
    int TotalMembers,
    int ActiveMembers,
    int Employees,
    int Managers,
    int Admins);

// ─── QUERIES ──────────────────────────────────────────────────────────────────

public record GetTeamMembersQuery(
    string? Search = null,
    string? Role = null,
    bool IncludeInactive = false)
    : IRequest<List<TeamMemberDto>>;

public class GetTeamMembersQueryHandler
    : IRequestHandler<GetTeamMembersQuery, List<TeamMemberDto>>
{
    private readonly IApplicationDbContext _context;

    public GetTeamMembersQueryHandler(IApplicationDbContext context)
        => _context = context;

    public async Task<List<TeamMemberDto>> Handle(
        GetTeamMembersQuery request, CancellationToken ct)
    {
        var users = await _context.Users
            .AsNoTracking()
            .Where(u => request.IncludeInactive || u.IsActive)
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.Role,
                u.IsActive,
                u.CreatedAt
            })
            .ToListAsync(ct);

        // Filter in memory to avoid EF translation issues
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            users = users.Where(u =>
                u.FirstName.ToLower().Contains(s) ||
                u.LastName.ToLower().Contains(s) ||
                u.Email.ToLower().Contains(s)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(request.Role) &&
            Enum.TryParse<UserRole>(request.Role, out var roleEnum))
            users = users.Where(u => u.Role == roleEnum).ToList();

        // Load expense stats in one query
        var userIds = users.Select(u => u.Id).ToList();

        var expenseStats = await _context.Expenses
            .AsNoTracking()
            .Where(e => userIds.Contains(e.SubmittedById))
            .Select(e => new { e.SubmittedById, e.Amount, e.Status })
            .ToListAsync(ct);

        var statsByUser = expenseStats
            .GroupBy(e => e.SubmittedById)
            .ToDictionary(g => g.Key, g => new
            {
                Total = g.Count(),
                Amount = g.Sum(e => e.Amount),
                Pending = g.Count(e => e.Status == ExpenseStatus.Submitted)
            });

        return users
            .OrderBy(u => u.FirstName)
            .Select(u =>
            {
                statsByUser.TryGetValue(u.Id, out var stats);
                return new TeamMemberDto(
                    u.Id, u.FirstName, u.LastName,
                    $"{u.FirstName} {u.LastName}",
                    u.Email, u.Role.ToString(), u.IsActive, u.CreatedAt,
                    stats?.Total ?? 0,
                    stats?.Amount ?? 0,
                    stats?.Pending ?? 0);
            })
            .ToList();
    }
}

public record GetTeamStatsQuery : IRequest<TeamStatsDto>;

public class GetTeamStatsQueryHandler
    : IRequestHandler<GetTeamStatsQuery, TeamStatsDto>
{
    private readonly IApplicationDbContext _context;

    public GetTeamStatsQueryHandler(IApplicationDbContext context)
        => _context = context;

    public async Task<TeamStatsDto> Handle(
        GetTeamStatsQuery request, CancellationToken ct)
    {
        var users = await _context.Users
            .AsNoTracking()
            .Select(u => new { u.Role, u.IsActive })
            .ToListAsync(ct);

        return new TeamStatsDto(
            users.Count,
            users.Count(u => u.IsActive),
            users.Count(u => u.Role == UserRole.Employee),
            users.Count(u => u.Role == UserRole.Manager),
            users.Count(u => u.Role == UserRole.Admin || u.Role == UserRole.SuperAdmin));
    }
}

public record GetTeamMemberByIdQuery(Guid Id) : IRequest<TeamMemberDto>;

public class GetTeamMemberByIdQueryHandler
    : IRequestHandler<GetTeamMemberByIdQuery, TeamMemberDto>
{
    private readonly IApplicationDbContext _context;

    public GetTeamMemberByIdQueryHandler(IApplicationDbContext context)
        => _context = context;

    public async Task<TeamMemberDto> Handle(
        GetTeamMemberByIdQuery request, CancellationToken ct)
    {
        var u = await _context.Users
            .AsNoTracking()
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.Role,
                u.IsActive,
                u.CreatedAt
            })
            .FirstOrDefaultAsync(u => u.Id == request.Id, ct)
            ?? throw new KeyNotFoundException("Team member not found.");

        var stats = await _context.Expenses
            .AsNoTracking()
            .Where(e => e.SubmittedById == u.Id)
            .Select(e => new { e.Amount, e.Status })
            .ToListAsync(ct);

        return new TeamMemberDto(
            u.Id, u.FirstName, u.LastName,
            $"{u.FirstName} {u.LastName}",
            u.Email, u.Role.ToString(), u.IsActive, u.CreatedAt,
            stats.Count,
            stats.Sum(e => e.Amount),
            stats.Count(e => e.Status == ExpenseStatus.Submitted));
    }
}

// ─── COMMANDS ─────────────────────────────────────────────────────────────────

public record InviteTeamMemberCommand(
    string FirstName,
    string LastName,
    string Email,
    string Role,
    string TemporaryPassword) : IRequest<Guid>;

public class InviteTeamMemberCommandHandler
    : IRequestHandler<InviteTeamMemberCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public InviteTeamMemberCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    { _context = context; _currentUser = currentUser; }

    public async Task<Guid> Handle(
        InviteTeamMemberCommand request, CancellationToken ct)
    {
        var email = request.Email.ToLower().Trim();

        if (await _context.Users.AnyAsync(u => u.Email == email, ct))
            throw new InvalidOperationException(
                $"A user with email '{email}' already exists in this organisation.");

        if (!Enum.TryParse<UserRole>(request.Role, out var role))
            throw new ArgumentException($"Invalid role '{request.Role}'.");

        // Admins cannot create SuperAdmins
        if (role == UserRole.SuperAdmin)
            throw new InvalidOperationException("Cannot assign SuperAdmin role.");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.TemporaryPassword);

        var user = User.Create(
            request.FirstName.Trim(),
            request.LastName.Trim(),
            email,
            passwordHash,
            _currentUser.TenantId,
            role);

        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);
        return user.Id;
    }
}

public record ChangeRoleCommand(Guid UserId, string NewRole) : IRequest;

public class ChangeRoleCommandHandler : IRequestHandler<ChangeRoleCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ChangeRoleCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    { _context = context; _currentUser = currentUser; }

    public async Task Handle(ChangeRoleCommand request, CancellationToken ct)
    {
        if (request.UserId == _currentUser.UserId)
            throw new InvalidOperationException("You cannot change your own role.");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        if (!Enum.TryParse<UserRole>(request.NewRole, out var role))
            throw new ArgumentException($"Invalid role '{request.NewRole}'.");

        if (role == UserRole.SuperAdmin)
            throw new InvalidOperationException("Cannot assign SuperAdmin role.");

        user.ChangeRole(role);
        await _context.SaveChangesAsync(ct);
    }
}

public record DeactivateTeamMemberCommand(Guid UserId) : IRequest;

public class DeactivateTeamMemberCommandHandler
    : IRequestHandler<DeactivateTeamMemberCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeactivateTeamMemberCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    { _context = context; _currentUser = currentUser; }

    public async Task Handle(DeactivateTeamMemberCommand request, CancellationToken ct)
    {
        if (request.UserId == _currentUser.UserId)
            throw new InvalidOperationException("You cannot deactivate your own account.");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        if (user.Role == UserRole.SuperAdmin)
            throw new InvalidOperationException("Cannot deactivate a SuperAdmin.");

        user.Deactivate();
        await _context.SaveChangesAsync(ct);
    }
}

public record ReactivateTeamMemberCommand(Guid UserId) : IRequest;

public class ReactivateTeamMemberCommandHandler
    : IRequestHandler<ReactivateTeamMemberCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ReactivateTeamMemberCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    { _context = context; _currentUser = currentUser; }

    public async Task Handle(ReactivateTeamMemberCommand request, CancellationToken ct)
    {
        var user = await _context.Users
            .IgnoreQueryFilters() // user may be filtered out because IsActive = false
            .FirstOrDefaultAsync(u => u.Id == request.UserId
                && u.TenantId == _currentUser.TenantId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        user.Activate();
        await _context.SaveChangesAsync(ct);
    }
}

public record ResetPasswordCommand(
    Guid UserId,
    string NewPassword) : IRequest;

public class ResetPasswordCommandHandler
    : IRequestHandler<ResetPasswordCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ResetPasswordCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    { _context = context; _currentUser = currentUser; }

    public async Task Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        if (request.UserId == _currentUser.UserId)
            throw new InvalidOperationException(
                "Use the change-password flow to reset your own password.");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        var hash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.ResetPassword(hash);
        user.ClearRefreshToken();
        await _context.SaveChangesAsync(ct);
    }
}