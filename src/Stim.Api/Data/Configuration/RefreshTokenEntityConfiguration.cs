using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stim.Api.Entities;

namespace Stim.Api.Data.Configuration;

public class RefreshTokenEntityConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(rt => rt.Id);

        builder.HasIndex(rt => rt.Token).IsUnique();

        builder.HasOne(rt => rt.User).WithMany().HasForeignKey(rt => rt.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.Property(rt => rt.Token).HasMaxLength(1000);

        builder.Property(rt => rt.UserId).HasMaxLength(300);
    }
}
