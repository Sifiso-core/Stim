using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stim.Api.Entities;

namespace Stim.Api.Data.Configuration;

public class GameTagEntityConfiguration : IEntityTypeConfiguration<GameTag>
{
    public void Configure(EntityTypeBuilder<GameTag> builder)
    {
        builder.HasKey(gt => new { gt.GameId, gt.TagId });

        builder.HasOne(gt => gt.Game)
            .WithMany(g => g.GameTags)
            .HasForeignKey(gt => gt.GameId).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(gt => gt.Tag)
            .WithMany()
            .HasForeignKey(gt => gt.TagId).OnDelete(DeleteBehavior.Cascade);

    }
}
