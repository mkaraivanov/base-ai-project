using Domain.Entities;

namespace Tests.Unit.Builders;

public class UserBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _email = "test@example.com";
    private string _passwordHash = BCrypt.Net.BCrypt.HashPassword("Password123");
    private string _firstName = "John";
    private string _lastName = "Doe";
    private string _phoneNumber = "+1234567890";
    private UserRole _role = UserRole.Customer;
    private bool _isActive = true;
    private DateTime _createdAt = DateTime.UtcNow;
    private DateTime _lastLoginAt = DateTime.UtcNow;

    public UserBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UserBuilder WithRole(UserRole role)
    {
        _role = role;
        return this;
    }

    public UserBuilder AsAdmin()
    {
        _role = UserRole.Admin;
        return this;
    }

    public UserBuilder AsInactive()
    {
        _isActive = false;
        return this;
    }

    public User Build()
    {
        return new User
        {
            Id = _id,
            Email = _email,
            PasswordHash = _passwordHash,
            FirstName = _firstName,
            LastName = _lastName,
            PhoneNumber = _phoneNumber,
            Role = _role,
            IsActive = _isActive,
            CreatedAt = _createdAt,
            LastLoginAt = _lastLoginAt
        };
    }
}
