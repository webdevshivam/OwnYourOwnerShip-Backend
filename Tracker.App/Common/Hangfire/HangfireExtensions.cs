using Hangfire;
using Hangfire.PostgreSql;
using Tracker.App.Common.Constants;

namespace Tracker.App.Common.Hangfire;

/// <summary>
/// Extension methods for configuring Hangfire with PostgreSQL storage,
/// background worker processing, and dashboard routing.
/// </summary>
public static class HangfireExtensions
{
    /// <summary>
    /// Adds and configures Hangfire services using PostgreSQL storage.
    /// </summary>
    public static IServiceCollection AddHangfireConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddHangfire(config =>
        {
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                  .UseSimpleAssemblyNameTypeSerializer()
                  .UseRecommendedSerializerSettings()
                  .UsePostgreSqlStorage(options =>
                  {
                      options.UseNpgsqlConnection(connectionString);
                  });
        });

        // Add Hangfire background worker server
        services.AddHangfireServer(options =>
        {
            options.WorkerCount = HangfireConstants.DefaultWorkerCount;
            options.Queues = new[] { "default", HangfireConstants.WebhookDispatchQueue };
        });

        return services;
    }

    /// <summary>
    /// Configures the Hangfire Dashboard UI with custom authorization.
    /// </summary>
    public static IApplicationBuilder UseHangfireDashboardCustom(this IApplicationBuilder app)
    {
        app.UseHangfireDashboard(HangfireConstants.DashboardPath, new DashboardOptions
        {
            DashboardTitle = HangfireConstants.DashboardTitle,
            Authorization = new[] { new HangfireDashboardAuthorizationFilter() }
        });

        return app;
    }
}
