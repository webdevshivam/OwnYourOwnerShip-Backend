using Tracker.App.Common.Constants;

namespace Tracker.App.Common.Models;

/// <summary>
/// Universal response envelope for all API endpoints returning data.
/// Guarantees consistent shape across all frontend and mobile client consumers.
/// </summary>
/// <typeparam name="T">The type of the payload payload.</typeparam>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<ValidationErrorDetail>? Errors { get; set; }
    public PaginationMetadata? Pagination { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    public ApiResponse()
    {
    }

    public ApiResponse(bool success, int statusCode, string message, T? data = default, List<ValidationErrorDetail>? errors = null, PaginationMetadata? pagination = null)
    {
        Success = success;
        StatusCode = statusCode;
        Message = message;
        Data = data;
        Errors = errors;
        Pagination = pagination;
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Creates a 200 OK success response envelope.
    /// </summary>
    public static ApiResponse<T> Ok(T data, string message = ApiConstants.SuccessMessage) =>
        new(true, 200, message, data);

    /// <summary>
    /// Creates a 201 Created success response envelope.
    /// </summary>
    public static ApiResponse<T> Created(T data, string message = ApiConstants.ResourceCreatedMessage) =>
        new(true, 201, message, data);

    /// <summary>
    /// Creates a 200 OK paginated success response envelope.
    /// </summary>
    public static ApiResponse<T> Paginated(T data, PaginationMetadata pagination, string message = ApiConstants.SuccessMessage) =>
        new(true, 200, message, data, null, pagination);

    /// <summary>
    /// Creates a failure response envelope with optional validation details.
    /// </summary>
    public static ApiResponse<T> Fail(string message, int statusCode = 400, List<ValidationErrorDetail>? errors = null) =>
        new(false, statusCode, message, default, errors);
}

/// <summary>
/// Non-generic response envelope for operations that do not return a data payload (e.g. DELETE, simple actions).
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    public ApiResponse()
    {
    }

    public ApiResponse(bool success, int statusCode, string message, List<ValidationErrorDetail>? errors = null)
        : base(success, statusCode, message, null, errors)
    {
    }

    public static ApiResponse Ok(string message = ApiConstants.DefaultSuccessMessage) =>
        new(true, 200, message);

    public static new ApiResponse Fail(string message, int statusCode = 400, List<ValidationErrorDetail>? errors = null) =>
        new(false, statusCode, message, errors);
}

/// <summary>
/// Details of an individual field-level validation issue.
/// </summary>
public record ValidationErrorDetail(string Field, string Message);

/// <summary>
/// Metadata accompanying paginated list responses.
/// </summary>
public record PaginationMetadata(
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage
);
