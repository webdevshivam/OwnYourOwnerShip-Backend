namespace Tracker.App.Features.Auth.DTOs;

/// <summary>
/// Minimal, ultra-lightweight authentication response.
/// Contains only the short-lived JWT Access Token and metadata.
/// 
/// Security & Architecture Note:
/// - RefreshToken is NOT included here; it is delivered exclusively via an HttpOnly secure cookie to block XSS theft.
/// - Redundant user profile fields are excluded; user identity claims (id, email, name) are already encoded inside the JWT.
/// </summary>
public record LoginResponse(
    string AccessToken,
    string TokenType,
    int ExpiresInSeconds
);
