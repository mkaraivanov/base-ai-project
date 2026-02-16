namespace Application.DTOs.Auth;

public record AuthResponseDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    string Token,
    DateTime ExpiresAt
);
