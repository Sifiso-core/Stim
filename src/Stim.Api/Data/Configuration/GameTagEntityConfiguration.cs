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

        builder.HasOne<Game>()
            .WithMany(g => g.GameTags)
            .HasForeignKey(gt => gt.GameId);

        builder.HasOne<Tag>()
            .WithMany()
            .HasForeignKey(gt => gt.TagId);

    }
}
