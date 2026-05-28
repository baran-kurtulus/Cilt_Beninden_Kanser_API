using Cilt_Beninden_Kanser_Api.Domain.Exceptions;

namespace Cilt_Beninden_Kanser_Api.Domain.Entities;

public class User
{
    private User() { }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string? FullName { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsActive { get; private set; }

    public static User Create(string email, string passwordHash, string? fullName)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("E-posta alanı boş olamaz.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Şifre hash değeri boş olamaz.");

        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public void Deactivate() => IsActive = false;
}
