using FinanceTracker.Application.Common.Exceptions;
using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using BC = BCrypt.Net.BCrypt;

namespace FinanceTracker.Application.Users;

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public record UserListDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string Role,
    bool IsActive,
    DateTime CreatedAt);

public record UserDetailDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string Role,
    bool IsActive,
    DateTime CreatedAt);

// ─── QUERIES ──────────────────────────────────────────────────────────────────

public record GetUsersListQuery(
    string? Search = null,
    string? Role = null,
    bool? IsActive = null) : IRequest<List<UserListDto>>;

public class GetUsersListQueryHandler
    : IRequestHandler<GetUsersListQuery, List<UserListDto>>
{
    private readonly IApplicationDbContext _context;

    public GetUsersListQueryHandler(IApplicationDbContext context)
        => _context = context;

    public async Task<List<UserListDto>> Handle(
        GetUsersListQuery request, CancellationToken ct)
    {
        var query = _context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            query = query.Where(u =>
                u.FirstName.ToLower().Contains(s) ||
                u.LastName.ToLower().Contains(s) ||
                u.Email.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(request.Role) &&
            Enum.TryParse<UserRole>(request.Role, out var role))
            query = query.Where(u => u.Role == role);

        if (request.IsActive.HasValue)
            query = query.Where(u => u.IsActive == request.IsActive.Value);

        return await query
            .OrderBy(u => u.FirstName)
            .Select(u => new UserListDto(
                u.Id, u.FirstName, u.LastName, u.FullName,
                u.Email, u.Role.ToString(), u.IsActive, u.CreatedAt))
            .ToListAsync(ct);
    }
}

public record GetUserByIdQuery(Guid Id) : IRequest<UserDetailDto>;

public class GetUserByIdQueryHandler
    : IRequestHandler<GetUserByIdQuery, UserDetailDto>
{
    private readonly IApplicationDbContext _context;

    public GetUserByIdQueryHandler(IApplicationDbContext context)
        => _context = context;

    public async Task<UserDetailDto> Handle(
        GetUserByIdQuery request, CancellationToken ct)
    {
        var u = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(User), request.Id);

        return new UserDetailDto(
            u.Id, u.FirstName, u.LastName, u.FullName,
            u.Email, u.Role.ToString(), u.IsActive, u.CreatedAt);
    }
}

// ─── COMMANDS ─────────────────────────────────────────────────────────────────

// Invite User (Admin creates account, sends temp password)
public record InviteUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string Role,
    string TemporaryPassword) : IRequest<Guid>;

public class InviteUserCommandValidator : AbstractValidator<InviteUserCommand>
{
    public InviteUserCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Role).NotEmpty()
            .Must(r => Enum.TryParse<UserRole>(r, out _))
            .WithMessage("Invalid role. Must be Employee, Manager, or Admin.");
        RuleFor(x => x.TemporaryPassword).NotEmpty().MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
    }
}

public class InviteUserCommandHandler : IRequestHandler<InviteUserCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public InviteUserCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    { _context = context; _currentUser = currentUser; }

    public async Task<Guid> Handle(InviteUserCommand request, CancellationToken ct)
    {
        // Only Admin/SuperAdmin can invite
        var callerRole = Enum.Parse<UserRole>(_currentUser.Role);
        if (callerRole < UserRole.Admin)
            throw new ForbiddenException("Only admins can invite users.");

        // Cannot invite SuperAdmin
        var targetRole = Enum.Parse<UserRole>(request.Role);
        if (targetRole == UserRole.SuperAdmin)
            throw new ForbiddenException("Cannot assign SuperAdmin role.");

        // Check email unique within tenant
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == request.Email.ToLower().Trim(), ct);
        if (emailExists)
            throw new InvalidOperationException(
                $"A user with email '{request.Email}' already exists.");

        var passwordHash = BC.HashPassword(request.TemporaryPassword);
        var user = User.Create(
            request.FirstName, request.LastName,
            request.Email, passwordHash,
            _currentUser.TenantId, targetRole);

        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);
        return user.Id;
    }
}

// Change Role
public record ChangeUserRoleCommand(Guid UserId, string NewRole) : IRequest;

public class ChangeUserRoleCommandValidator : AbstractValidator<ChangeUserRoleCommand>
{
    public ChangeUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NewRole).NotEmpty()
            .Must(r => Enum.TryParse<UserRole>(r, out _))
            .WithMessage("Invalid role.");
    }
}

public class ChangeUserRoleCommandHandler : IRequestHandler<ChangeUserRoleCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ChangeUserRoleCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    { _context = context; _currentUser = currentUser; }

    public async Task Handle(ChangeUserRoleCommand request, CancellationToken ct)
    {
        var callerRole = Enum.Parse<UserRole>(_currentUser.Role);
        if (callerRole < UserRole.Admin)
            throw new ForbiddenException("Only admins can change roles.");

        var newRole = Enum.Parse<UserRole>(request.NewRole);
        if (newRole == UserRole.SuperAdmin)
            throw new ForbiddenException("Cannot assign SuperAdmin role.");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        // Cannot change your own role
        if (user.Id == _currentUser.UserId)
            throw new InvalidOperationException("You cannot change your own role.");

        user.ChangeRole(newRole);
        await _context.SaveChangesAsync(ct);
    }
}

// Deactivate User
public record DeactivateUserCommand(Guid UserId) : IRequest;

public class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeactivateUserCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    { _context = context; _currentUser = currentUser; }

    public async Task Handle(DeactivateUserCommand request, CancellationToken ct)
    {
        var callerRole = Enum.Parse<UserRole>(_currentUser.Role);
        if (callerRole < UserRole.Admin)
            throw new ForbiddenException("Only admins can deactivate users.");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        if (user.Id == _currentUser.UserId)
            throw new InvalidOperationException("You cannot deactivate your own account.");

        if (user.Role == UserRole.SuperAdmin)
            throw new ForbiddenException("Cannot deactivate a SuperAdmin.");

        user.Deactivate();
        await _context.SaveChangesAsync(ct);
    }
}

// Reactivate User
public record ReactivateUserCommand(Guid UserId) : IRequest;

public class ReactivateUserCommandHandler : IRequestHandler<ReactivateUserCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ReactivateUserCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    { _context = context; _currentUser = currentUser; }

    public async Task Handle(ReactivateUserCommand request, CancellationToken ct)
    {
        var callerRole = Enum.Parse<UserRole>(_currentUser.Role);
        if (callerRole < UserRole.Admin)
            throw new ForbiddenException("Only admins can reactivate users.");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        user.Activate();
        await _context.SaveChangesAsync(ct);
    }
}

// Update Own Profile
public record UpdateProfileCommand(
    string FirstName,
    string LastName) : IRequest;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
    }
}

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateProfileCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    { _context = context; _currentUser = currentUser; }

    public async Task Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, ct)
            ?? throw new NotFoundException(nameof(User), _currentUser.UserId);

        user.UpdateProfile(request.FirstName, request.LastName);
        await _context.SaveChangesAsync(ct);
    }
}

// Change Own Password
public record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword) : IRequest;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("New password must be different from current password.");
    }
}

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ChangePasswordCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    { _context = context; _currentUser = currentUser; }

    public async Task Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, ct)
            ?? throw new NotFoundException(nameof(User), _currentUser.UserId);

        if (!BC.Verify(request.CurrentPassword, user.PasswordHash))
            throw new InvalidOperationException("Current password is incorrect.");

        user.ChangePassword(BC.HashPassword(request.NewPassword));
        await _context.SaveChangesAsync(ct);
    }
}