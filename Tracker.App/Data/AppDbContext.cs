using Microsoft.EntityFrameworkCore;
using Tracker.App.Common.Entities;
using Tracker.App.Data.Entities;

namespace Tracker.App.Data;

/// <summary>
/// Primary Entity Framework Core database context for OwnYourOwnership.
/// Configured for PostgreSQL with clean, modular entity mappings.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // ==========================================
    // DbSets for all Application Entities
    // ==========================================
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<RecurrenceRule> RecurrenceRules => Set<RecurrenceRule>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<TaskTag> TaskTags => Set<TaskTag>();
    public DbSet<TaskActivityLog> TaskActivityLogs => Set<TaskActivityLog>();
    public DbSet<DailyPlan> DailyPlans => Set<DailyPlan>();
    public DbSet<DailyPlanItem> DailyPlanItems => Set<DailyPlanItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Automatically loads and applies all IEntityTypeConfiguration classes from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    /// <summary>
    /// Automatically manages UpdatedAt timestamps whenever an entity is modified.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        var now = DateTimeOffset.UtcNow;

        foreach (var entry in entries)
        {
            var entity = (BaseEntity)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = now;
            }

            entity.UpdatedAt = now;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
