using MemberService.Domain.ValueObjects;

namespace MemberService.Domain.Entities;

/// <summary>
/// User entity.
/// Represents a registered user in the system.
/// </summary>
public class User
{
    public long Id { get; private set; }
    public Email Email { get; private set; }
    public string PasswordHash { get; private set; }
    public Username Username { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation property
    public ICollection<RefreshToken> RefreshTokens { get; } = new List<RefreshToken>();
    
    // Constructor for EF Core
    private User()
    {
        Email = null!;
        PasswordHash = null!;
        Username = null!;
    }
    
    /// <summary>
    /// Creates a new User entity.
    /// </summary>
    /// <param name="id">Snowflake ID generated identifier</param>
    /// <param name="email">User email value object</param>
    /// <param name="username">User display name value object</param>
    /// <param name="passwordHash">bcrypt password hash</param>
    /// <returns>A new User entity</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
    public static User Create(long id, Email email, Username username, string passwordHash)
    {
        if (id <= 0)
            throw new ArgumentException("使用者ID必須大於0", nameof(id));
        
        if (email == null)
            throw new ArgumentNullException(nameof(email));
        
        if (username == null)
            throw new ArgumentNullException(nameof(username));
        
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("密碼雜湊不能為空", nameof(passwordHash));
        
        var now = DateTime.UtcNow;
        
        return new User
        {
            Id = id,
            Email = email,
            Username = username,
            PasswordHash = passwordHash,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
    
    /// <summary>
    /// Updates the user's password hash.
    /// </summary>
    /// <param name="newPasswordHash">The new bcrypt password hash</param>
    public void UpdatePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("新密碼雜湊不能為空", nameof(newPasswordHash));
        
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Updates the user's display name.
    /// </summary>
    /// <param name="newUsername">The new username value object</param>
    public void UpdateUsername(Username newUsername)
    {
        if (newUsername == null)
            throw new ArgumentNullException(nameof(newUsername));
        
        Username = newUsername;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Updates the user's email address.
    /// </summary>
    /// <param name="newEmail">The new email value object</param>
    public void UpdateEmail(Email newEmail)
    {
        if (newEmail == null)
            throw new ArgumentNullException(nameof(newEmail));
        
        Email = newEmail;
        UpdatedAt = DateTime.UtcNow;
    }
}
