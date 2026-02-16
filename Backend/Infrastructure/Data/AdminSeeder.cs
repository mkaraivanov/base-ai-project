using System;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public static class AdminSeeder
    {
        public static async Task SeedAdminUserAsync(CinemaDbContext context)
        {
            var adminEmail = "admin@cinebook.local";
            var existing = await context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
            if (existing != null) return;

            var password = "Admin123!";
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            var admin = new User
            {
                Id = Guid.NewGuid(),
                Email = adminEmail,
                PasswordHash = passwordHash,
                FirstName = "Admin",
                LastName = "User",
                PhoneNumber = "+10000000000",
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };
            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }
    }
}
