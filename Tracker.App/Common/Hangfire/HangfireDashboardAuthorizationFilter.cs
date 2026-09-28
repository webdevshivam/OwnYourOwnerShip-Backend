using Hangfire.Dashboard;

namespace Tracker.App.Common.Hangfire;

/// <summary>
/// Custom authorization filter for the Hangfire Dashboard.
/// Protects the dashboard from unauthorized public access.
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // 1. Allow local requests in Development
        var env = httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        if (env.IsDevelopment())
        {
            return true;
        }

        // 2. In Production: Enforce authenticated admin access
        return httpContext.User.Identity?.IsAuthenticated == true &&
               httpContext.User.IsInRole("Admin");
    }
}
