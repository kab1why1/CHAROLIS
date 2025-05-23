using charolis.DAL;
using charolis.Entities;
using BCrypt.Net;
using System.Linq;

namespace charolis.Entities
{
    public static class AdminInitializer
    {
        public static void Initialize(EfContext context)
        {
            // Якщо в базі немає жодного адміна — створюємо
            if (!context.Users.Any(u => u.Role == "Admin"))
            {
                var admin = new User
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    Role     = "Admin",
                    Email    = "admin@example.com",
                    PhoneNumber = "+380000000000",
                    Address  = "Адмінська адреса"
                };

                context.Users.Add(admin);
                context.SaveChanges();
            }
        }
    }
}