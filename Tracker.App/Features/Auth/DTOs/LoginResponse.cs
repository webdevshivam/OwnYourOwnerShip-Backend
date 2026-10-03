namespace Tracker.App.Features.Auth.DTOs;

/// <summary>
/// User identity summary delivered to the client upon successful authentication.
/// </summary>
public record AuthUserDto(
    Guid Id,
    string Email,
    string FullName
);

/// <summary>
/// Client authentication response envelope.
/// Sensitive tokens (both AccessToken and RefreshToken) are delivered strictly via secure HttpOnly cookies.
/// Contains user profile and session metadata needed by the frontend application.
/// </summary>
public record LoginResponse(
    AuthUserDto User,
    int ExpiresInSeconds,
    string TokenType = "Bearer"
);
