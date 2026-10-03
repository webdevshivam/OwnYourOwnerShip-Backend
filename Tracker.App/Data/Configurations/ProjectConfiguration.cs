using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tracker.App.Common.Constants;
using Tracker.App.Data.Entities;

namespace Tracker.App.Data.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable(DatabaseConstants.Tables.Projects);

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(ValidationConstants.ProjectNameMaxLength);

        builder.Property(p => p.ColorHex)
            .IsRequired()
            .HasMaxLength(ValidationConstants.HexColorLength)
            .HasDefaultValue(DatabaseConstants.Defaults.ProjectDefaultColorHex);

        builder.Property(p => p.SortOrder)
            .HasDefaultValue(0);

        builder.Property(p => p.IsArchived)
            .HasDefaultValue(false);

        builder.HasIndex(p => new { p.UserId, p.IsArchived });
    }
}
