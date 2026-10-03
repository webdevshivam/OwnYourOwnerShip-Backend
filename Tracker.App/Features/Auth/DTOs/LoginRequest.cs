using System.ComponentModel.DataAnnotations;
using Tracker.App.Common.Constants;

namespace Tracker.App.Features.Auth.DTOs;

/// <summary>
/// Data transfer object for user login credentials.
/// Validation rules and messages are centralized in ValidationConstants.
/// </summary>
public record LoginRequest(
    [Required(ErrorMessage = ValidationConstants.EmailRequiredErrorMessage)]
    [EmailAddress(ErrorMessage = ValidationConstants.InvalidEmailErrorMessage)]
    [MaxLength(ValidationConstants.EmailMaxLength, ErrorMessage = ValidationConstants.EmailMaxLengthErrorMessage)]
    string Email,

    [Required(ErrorMessage = ValidationConstants.PasswordRequiredErrorMessage)]
    [MinLength(6, ErrorMessage = ValidationConstants.PasswordMinLengthErrorMessage)]
    string Password
);
