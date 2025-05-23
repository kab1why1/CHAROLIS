using charolis.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace charolis.DAL.Configuration;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired();
        
        builder.Property(p => p.Description)
            .HasMaxLength(200);
        
        builder.Property(p => p.Price)
            .IsRequired()
            .HasColumnType("decimal(10,2)");
        
        builder.Property(p => p.IsAvailable)
            .IsRequired();
        
        builder.ToTable("Products");
    }
}