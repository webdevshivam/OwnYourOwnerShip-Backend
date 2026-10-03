using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tracker.App.Common.Constants;
using Tracker.App.Data.Entities;

namespace Tracker.App.Data.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable(DatabaseConstants.Tables.Tags);

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(ValidationConstants.TagNameMaxLength);

        builder.Property(t => t.ColorHex)
            .IsRequired()
            .HasMaxLength(ValidationConstants.HexColorLength)
            .HasDefaultValue(DatabaseConstants.Defaults.TagDefaultColorHex);

        // A user cannot have duplicate tag names
        builder.HasIndex(t => new { t.UserId, t.Name })
            .IsUnique();
    }
}
