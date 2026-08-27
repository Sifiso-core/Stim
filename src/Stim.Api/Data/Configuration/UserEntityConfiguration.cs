using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stim.Api.Entities;

namespace Stim.Api.Data.Configuration;

public class UserEntityConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasMaxLength(500);

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.IdentityId).IsUnique();

        builder.Property(u => u.Email).HasMaxLength(300);
        builder.Property(u => u.IdentityId).HasMaxLength(500);
    }
}
