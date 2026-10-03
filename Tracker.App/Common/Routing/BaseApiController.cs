using Microsoft.AspNetCore.Mvc;
using Tracker.App.Common.Constants;
using Tracker.App.Common.Models;

namespace Tracker.App.Common.Routing;

/// <summary>
/// Base API controller for Tracker.App endpoints.
/// 
/// Inheriting controllers automatically receive:
///  1. [ApiController] behavior (automatic model validation, 400 Bad Request responses, parameter source inference).
///  2. Standard "api/[controller]" route prefix by default.
///  3. Built-in helpers for producing consistent ApiResponse envelopes.
/// 
/// Custom route naming:
///  - To specify a custom API route name, annotate your derived controller with:
///    [ApiRoute("custom-name")]   (e.g., [ApiRoute("auth")] -> produces route "api/auth")
///    or standard ASP.NET Core: [Route("api/custom-name")].
/// </summary>
[ApiController]
[ApiRoute]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Returns a 200 OK result wrapped in a consistent ApiResponse envelope.
    /// </summary>
    protected IActionResult Success<T>(T data, string message = ApiConstants.SuccessMessage, int statusCode = StatusCodes.Status200OK)
    {
        return StatusCode(statusCode, ApiResponse<T>.Ok(data, message));
    }

    /// <summary>
    /// Returns a 200 OK result without data wrapped in a consistent ApiResponse envelope.
    /// </summary>
    protected IActionResult SuccessMessage(string message = ApiConstants.DefaultSuccessMessage, int statusCode = StatusCodes.Status200OK)
    {
        return StatusCode(statusCode, ApiResponse.Ok(message));
    }

    /// <summary>
    /// Returns a 201 Created result wrapped in a consistent ApiResponse envelope.
    /// </summary>
    protected IActionResult Created<T>(T data, string message = ApiConstants.ResourceCreatedMessage)
    {
        return StatusCode(StatusCodes.Status201Created, ApiResponse<T>.Created(data, message));
    }

    /// <summary>
    /// Returns a 200 OK paginated result wrapped in a consistent ApiResponse envelope.
    /// </summary>
    protected IActionResult Paginated<T>(T data, PaginationMetadata pagination, string message = ApiConstants.SuccessMessage)
    {
        return Ok(ApiResponse<T>.Paginated(data, pagination, message));
    }

    /// <summary>
    /// Returns an error result wrapped in a consistent ApiResponse envelope.
    /// </summary>
    protected IActionResult Failure(string message, int statusCode = StatusCodes.Status400BadRequest, List<ValidationErrorDetail>? errors = null)
    {
        return StatusCode(statusCode, ApiResponse.Fail(message, statusCode, errors));
    }
}
