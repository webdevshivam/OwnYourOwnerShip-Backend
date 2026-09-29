namespace Tracker.App.Common.Constants;

/// <summary>
/// Centralized validation rules, regular expressions, and field length constraints.
/// Reusable across DTO validations, EF Core configurations, and business logic.
/// </summary>
public static class ValidationConstants
{
    // ==========================================
    // Regular Expressions
    // ==========================================
    
    /// <summary>
    /// RFC 5322 compliant simplified email format regex.
    /// </summary>
    public const string EmailRegexPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

    /// <summary>
    /// Hexadecimal color format regex (e.g. #3B82F6 or #FFF).
    /// </summary>
    public const string HexColorRegexPattern = @"^#(?:[0-9a-fA-F]{3}){1,2}$";

    /// <summary>
    /// Strong password regex: Minimum 8 characters, at least 1 uppercase, 1 lowercase, 1 number.
    /// </summary>
    public const string PasswordComplexityPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$";

    // ==========================================
    // String Length Constraints
    // ==========================================
    
    public const int EmailMaxLength = 255;
    public const int PasswordHashMaxLength = 255;
    public const int FullNameMaxLength = 100;

    public const int ProjectNameMaxLength = 100;
    public const int TagNameMaxLength = 50;
    public const int HexColorLength = 7; // #RRGGBB

    public const int TaskTitleMaxLength = 250;
    public const int RecurrenceTitleMaxLength = 200;
    public const int DaysOfWeekMaskMaxLength = 30; // e.g. "MON,WED,FRI"

    public const int RecommendationReasonMaxLength = 250;

    public const int TokenHashLength = 64; // SHA-256 produces 64 hexadecimal characters
    public const int IpAddressMaxLength = 45; // Max length for IPv6 mapped IPv4 strings
    public const int DeviceInfoMaxLength = 255;

    // ==========================================
    // Friendly Validation Error Messages
    // ==========================================
    
    public const string RequiredFieldErrorMessage = "This field is required.";
    public const string EmailRequiredErrorMessage = "Email is required.";
    public const string InvalidEmailErrorMessage = "Please provide a valid email address.";
    public const string EmailMaxLengthErrorMessage = "Email cannot exceed 255 characters.";
    public const string PasswordRequiredErrorMessage = "Password is required.";
    public const string PasswordMinLengthErrorMessage = "Password must be at least 6 characters long.";
    public const string WeakPasswordErrorMessage = "Password must be at least 8 characters long and contain uppercase, lowercase, and numeric characters.";
    public const string InvalidCredentialsErrorMessage = "Invalid email or password.";
    public const string AccountLockedErrorMessage = "User account is temporarily locked out due to multiple failed login attempts.";
    public const string AccountInactiveErrorMessage = "User account is deactivated. Please contact support.";
    public const string InvalidHexColorErrorMessage = "Color must be a valid hex code (e.g., #3B82F6).";
}
