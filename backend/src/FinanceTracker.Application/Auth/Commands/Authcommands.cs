using FinanceTracker.Application.Common.Exceptions;
using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Application.Auth.Commands;

// ═══════════════════════════════════════════════════════════════════════════════
// DTOs
// ═══════════════════════════════════════════════════════════════════════════════
public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User);

public record UserDto(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    Guid TenantId);

// ═══════════════════════════════════════════════════════════════════════════════
// REGISTER TENANT + ADMIN USER
// ═══════════════════════════════════════════════════════════════════════════════
public record RegisterTenantCommand(
    string CompanyName,
    string Subdomain,
    string AdminFirstName,
    string AdminLastName,
    string AdminEmail,
    string Password) : IRequest<AuthResponseDto>;

public class RegisterTenantCommandValidator : AbstractValidator<RegisterTenantCommand>
{
    public RegisterTenantCommandValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Subdomain).NotEmpty().MaximumLength(50)
            .Matches("^[a-z0-9-]+$").WithMessage("Subdomain can only contain lowercase letters, numbers and hyphens.");
        RuleFor(x => x.AdminFirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AdminLastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
    }
}

public class RegisterTenantCommandHandler : IRequestHandler<RegisterTenantCommand, AuthResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenService _jwtService;

    public RegisterTenantCommandHandler(IApplicationDbContext context, IJwtTokenService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    public async Task<AuthResponseDto> Handle(RegisterTenantCommand request, CancellationToken ct)
    {
        // Check subdomain uniqueness
        if (await _context.Tenants.AnyAsync(t => t.Subdomain == request.Subdomain.ToLower(), ct))
            throw new ConflictException($"Subdomain '{request.Subdomain}' is already taken.");

        // Check email uniqueness
        if (await _context.Users.AnyAsync(u => u.Email == request.AdminEmail.ToLower(), ct))
            throw new ConflictException("A user with this email already exists.");

        // Create tenant
        var tenant = Tenant.Create(request.CompanyName, request.Subdomain);

        // Create admin user
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var admin = User.Create(
            request.AdminFirstName,
            request.AdminLastName,
            request.AdminEmail,
            passwordHash,
            tenant.Id,
            UserRole.Admin);

        // Default categories — seeded for every new tenant
        var defaultCategories = new[]
        {
            Category.Create("Travel",           tenant.Id, "#3b82f6", "airplane"),
            Category.Create("Food & Dining",    tenant.Id, "#f59e0b", "utensils"),
            Category.Create("Software & Tools", tenant.Id, "#8b5cf6", "computer"),
            Category.Create("Office Supplies",  tenant.Id, "#10b981", "briefcase"),
            Category.Create("Marketing",        tenant.Id, "#ef4444", "megaphone"),
            Category.Create("Training",         tenant.Id, "#06b6d4", "book"),
            Category.Create("Entertainment",    tenant.Id, "#ec4899", "music"),
            Category.Create("Utilities",        tenant.Id, "#64748b", "bolt"),
            Category.Create("Healthcare",       tenant.Id, "#22c55e", "heart"),
            Category.Create("Other",            tenant.Id, "#94a3b8", "folder"),
        };

        _context.Tenants.Add(tenant);
        _context.Users.Add(admin);
        _context.Categories.AddRange(defaultCategories);
        await _context.SaveChangesAsync(ct);

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(admin);
        var refreshToken = _jwtService.GenerateRefreshToken();
        admin.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));
        await _context.SaveChangesAsync(ct);

        return new AuthResponseDto(
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddMinutes(60),
            new UserDto(admin.Id, admin.FullName, admin.Email, admin.Role.ToString(), admin.TenantId));
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// LOGIN
// ═══════════════════════════════════════════════════════════════════════════════
public record LoginCommand(string Email, string Password) : IRequest<AuthResponseDto>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenService _jwtService;

    public LoginCommandHandler(IApplicationDbContext context, IJwtTokenService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await _context.Users
            .IgnoreQueryFilters() // bypass tenant filter for login
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower(), ct)
            ?? throw new ForbiddenException("Invalid email or password.");

        if (!user.IsActive)
            throw new ForbiddenException("Your account has been deactivated.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new ForbiddenException("Invalid email or password.");

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();
        user.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));
        await _context.SaveChangesAsync(ct);

        return new AuthResponseDto(
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddMinutes(60),
            new UserDto(user.Id, user.FullName, user.Email, user.Role.ToString(), user.TenantId));
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// REFRESH TOKEN
// ═══════════════════════════════════════════════════════════════════════════════
public record RefreshTokenCommand(string AccessToken, string RefreshToken) : IRequest<AuthResponseDto>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenService _jwtService;

    public RefreshTokenCommandHandler(IApplicationDbContext context, IJwtTokenService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var userId = _jwtService.GetUserIdFromExpiredToken(request.AccessToken)
            ?? throw new ForbiddenException("Invalid access token.");

        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new ForbiddenException("User not found.");

        if (user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiry < DateTime.UtcNow)
            throw new ForbiddenException("Invalid or expired refresh token.");

        var newAccessToken = _jwtService.GenerateAccessToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();
        user.SetRefreshToken(newRefreshToken, DateTime.UtcNow.AddDays(7));
        await _context.SaveChangesAsync(ct);

        return new AuthResponseDto(
            newAccessToken,
            newRefreshToken,
            DateTime.UtcNow.AddMinutes(60),
            new UserDto(user.Id, user.FullName, user.Email, user.Role.ToString(), user.TenantId));
    }
}