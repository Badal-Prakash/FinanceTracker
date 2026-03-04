using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Enums;

namespace FinanceTracker.Domain.Entities;

public class User : BaseEntity
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; } = UserRole.Employee;
    public bool IsActive { get; private set; } = true;
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiry { get; private set; }

    public string FullName => $"{FirstName} {LastName}";

    private User() { }

    public static User Create(string firstName, string lastName, string email,
        string passwordHash, Guid tenantId, UserRole role = UserRole.Employee)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = firstName,
            LastName = lastName,
            Email = email.ToLower().Trim(),
            PasswordHash = passwordHash,
            Role = role,
            IsActive = true
        };
    }

    public void SetRefreshToken(string token, DateTime expiry)
    {
        RefreshToken = token;
        RefreshTokenExpiry = expiry;
    }

    public void ClearRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiry = null;
    }

    public void ChangeRole(UserRole newRole) => Role = newRole;
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    public void UpdateProfile(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public void ChangePassword(string newPasswordHash)
        => PasswordHash = newPasswordHash;
}