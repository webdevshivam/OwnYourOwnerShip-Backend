using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tracker.App.Common.Constants;
using Tracker.App.Data.Entities;

namespace Tracker.App.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable(DatabaseConstants.Tables.RefreshTokens);

        builder.HasKey(r => r.Id);

        builder.Property(r => r.TokenHash)
            .IsRequired()
            .HasMaxLength(ValidationConstants.TokenHashLength);

        // Unique index for token lookup during refresh flow
        builder.HasIndex(r => r.TokenHash)
            .IsUnique();

        builder.Property(r => r.DeviceInfo)
            .HasMaxLength(ValidationConstants.DeviceInfoMaxLength);

        builder.Property(r => r.IpAddress)
            .HasMaxLength(ValidationConstants.IpAddressMaxLength);

        builder.Property(r => r.IsRevoked)
            .HasDefaultValue(false);

        // Self-referencing link for token rotation (replaced by)
        builder.HasOne(r => r.ReplacedByToken)
            .WithMany()
            .HasForeignKey(r => r.ReplacedByTokenId)
            .OnDelete(DeleteBehavior.SetNull);

        // Filtered index for listing active user sessions
        builder.HasIndex(r => new { r.UserId, r.IsRevoked });
    }
}
