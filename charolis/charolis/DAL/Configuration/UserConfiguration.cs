using charolis.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace charolis.DAL.Configuration;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Username)
            .IsRequired();
        builder.Property(u => u.Email);
        builder.Property(u => u.PasswordHash)
            .IsRequired();
        builder.Property(u => u.Role);
        builder.Property(u => u.PhoneNumber);
        builder.Property(u => u.Address);

        builder.Property(u => u.Balance)
            .HasPrecision(18, 2)
            .IsRequired()
            .HasDefaultValue(100m);
        
        builder.ToTable("Users");
    }
}