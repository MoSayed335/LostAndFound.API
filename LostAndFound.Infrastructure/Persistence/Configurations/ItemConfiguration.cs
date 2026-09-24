using LostAndFound.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LostAndFound.Infrastructure.Persistence.Configurations;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("Items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Title)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(i => i.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(i => i.Location)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.ImageUrl)
            .HasMaxLength(500);

        // User -> Items (Restrict: deleting a user should not silently wipe out
        // items they reported; that item history should be handled explicitly)
        builder.HasOne(i => i.User)
            .WithMany(u => u.Items)
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Category -> Items (Restrict: prevents deleting a category that still has items;
        // an admin must reassign/remove items first)
        builder.HasOne(i => i.Category)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes to support GET /api/items filtering without full table scans
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.Type);
        builder.HasIndex(i => i.Location);
        builder.HasIndex(i => i.CategoryId);
        builder.HasIndex(i => i.UserId);
        builder.HasIndex(i => i.DateLostOrFound);
        builder.HasIndex(i => new { i.Type, i.Status, i.CategoryId });
    }
}
