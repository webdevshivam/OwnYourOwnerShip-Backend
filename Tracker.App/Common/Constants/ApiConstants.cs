namespace Tracker.App.Common.Constants;

/// <summary>
/// Centralized API constants including response messages, route names, cookie names, and tokens.
/// Eliminates hardcoded magic strings across controllers, models, and configuration.
/// </summary>
public static class ApiConstants
{
    // ==========================================
    // Response Status Messages
    // ==========================================
    public const string DefaultSuccessMessage = "Operation completed successfully.";
    public const string SuccessMessage = "Success";
    public const string ResourceCreatedMessage = "Resource created successfully.";
    public const string ValidationFailedMessage = "One or more validation errors occurred.";
    public const string InvalidValueMessage = "Invalid value.";

    // ==========================================
    // Authentication & Token Constants
    // ==========================================
    public const string LoginSuccessMessage = "Login successful.";
    public const string LogoutSuccessMessage = "Logged out successfully.";
    public const string TokenRefreshedMessage = "Token refreshed successfully.";
    public const string MissingRefreshTokenMessage = "Refresh token is missing from request cookies.";
    public const string InvalidRefreshTokenMessage = "Invalid or expired refresh token.";
    public const string CompromisedTokenMessage = "Compromised token detected. All active sessions have been terminated for security.";
    
    public const string BearerScheme = "Bearer";
    public const string AccessTokenCookieName = "accessToken";
    public const string RefreshTokenCookieName = "refreshToken";
    public const string AuthCookiePath = "/";
    public const string AuthRoutePrefix = "auth";
    public const string LoginRoute = "login";
    public const string RefreshTokenRoute = "refresh-token";
    public const string LogoutRoute = "logout";

    // ==========================================
    // Cache Controller Messages
    // ==========================================
    public const string CacheRoutePrefix = "cache";
    public const string CacheKeyEmptyMessage = "Cache key cannot be empty.";
    public const string CacheKeyNotFoundMessage = "Key '{0}' not found in cache.";
    public const string CacheKeySavedMessage = "Key '{0}' successfully saved to cache.";
    public const string CacheKeyDeletedMessage = "Key '{0}' deleted successfully.";
}
