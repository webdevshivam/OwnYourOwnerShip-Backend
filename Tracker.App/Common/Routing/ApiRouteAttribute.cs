using Microsoft.AspNetCore.Mvc;

namespace Tracker.App.Common.Routing;

/// <summary>
/// Custom route attribute ensuring the "api" prefix is consistently preserved,
/// while allowing developers to specify a custom API route name or default to "[controller]".
/// 
/// Examples:
///   - [ApiRoute]                   => "api/[controller]" (e.g., "api/auth")
///   - [ApiRoute("auth")]           => "api/auth"
///   - [ApiRoute("custom-name")]    => "api/custom-name"
///   - [ApiRoute("api/auth")]       => "api/auth" (prevents duplicate "api/api" prefix)
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ApiRouteAttribute : RouteAttribute
{
    public const string DefaultPrefix = "api";

    public ApiRouteAttribute(string customName = "[controller]")
        : base(NormalizeRoute(customName))
    {
    }

    private static string NormalizeRoute(string template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return DefaultPrefix;
        }

        var trimmed = template.Trim().TrimStart('/');
        return trimmed.StartsWith(DefaultPrefix + "/", StringComparison.OrdinalIgnoreCase) ||
               trimmed.Equals(DefaultPrefix, StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : $"{DefaultPrefix}/{trimmed}";
    }
}
