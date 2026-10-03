namespace Tracker.App.Features.Auth.DTOs;

/// <summary>
/// Internal result envelope returned by AuthService to AuthController.
/// Carries the client LoginResponse and the raw AccessToken & RefreshToken for HttpOnly cookie injection.
/// </summary>
public record AuthServiceResult(
    bool IsSuccess,
    LoginResponse? Data = null,
    string? AccessToken = null,
    string? RefreshToken = null,
    string? ErrorMessage = null,
    int StatusCode = 200
)
{
    public static AuthServiceResult Success(LoginResponse data, string accessToken, string refreshToken) =>
        new(true, Data: data, AccessToken: accessToken, RefreshToken: refreshToken, StatusCode: 200);

    public static AuthServiceResult Success() =>
        new(true, StatusCode: 200);

    public static AuthServiceResult Failure(string errorMessage, int statusCode = 400) =>
        new(false, ErrorMessage: errorMessage, StatusCode: statusCode);
}
