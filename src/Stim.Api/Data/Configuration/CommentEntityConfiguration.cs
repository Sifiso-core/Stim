using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stim.Api.Entities;

namespace Stim.Api.Data.Configuration;

public class CommentEntityConfiguration : IEntityTypeConfiguration<Comments>
{
    public void Configure(EntityTypeBuilder<Comments> builder)
    {
        builder.ToTable("Comments");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasMaxLength(300);

        builder.Property(c => c.CommentText)
            .IsRequired()
            .HasMaxLength(1000);

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .HasPrincipalKey(c => c.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Game>()
            .WithMany()
            .HasForeignKey(c => c.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.GameId);
        builder.HasIndex(c => new { c.GameId, c.UserId });
    }
}
