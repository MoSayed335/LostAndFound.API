using LostAndFound.Domain.Entities;
using LostAndFound.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LostAndFound.Infrastructure.Persistence.Configurations;

public class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.ToTable("Claims");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Message)
            .IsRequired()
            .HasMaxLength(1000);

        // Item -> Claims (Cascade: a claim has no meaning once its item is gone)
        builder.HasOne(c => c.Item)
            .WithMany(i => i.Claims)
            .HasForeignKey(c => c.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // User -> Claims (Restrict: keep claim history even if we later restrict user deletion;
        // also avoids two cascade paths converging on the same table, which SQL Server rejects)
        builder.HasOne(c => c.Claimant)
            .WithMany(u => u.Claims)
            .HasForeignKey(c => c.ClaimantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.ItemId);
        builder.HasIndex(c => c.ClaimantId);
        builder.HasIndex(c => c.Status);

        // Enforces "a user cannot have multiple pending claims on the same item" at the
        // database level (filtered unique index - only applies to Pending rows).
        builder.HasIndex(c => new { c.ItemId, c.ClaimantId })
            .HasFilter($"[Status] = {(int)ClaimStatus.Pending}")
            .IsUnique();
    }
}
