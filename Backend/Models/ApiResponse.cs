namespace Backend.Models;

public record ApiResponse<T>(
    bool Success,
    T? Data,
    string? Error,
    List<string>? Errors = null
);
