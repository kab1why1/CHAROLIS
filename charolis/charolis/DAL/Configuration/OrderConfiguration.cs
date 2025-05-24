using charolis.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace charolis.DAL.Configuration;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("NOW()");

        builder.Property(o => o.Total)
            .HasPrecision(10, 2)
            .IsRequired();
        
        builder.Property(o => o.IsPaid)
            .IsRequired()
            .HasDefaultValue(false);
        
        builder.Property(o => o.PaidAt);
        
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.ToTable("Orders");
    }
}