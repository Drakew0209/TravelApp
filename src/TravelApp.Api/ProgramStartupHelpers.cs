using Microsoft.EntityFrameworkCore;
using TravelApp.Domain.Entities;
using TravelApp.Infrastructure.Persistence;

public static class ProgramStartupHelpers
{
    public static async Task EnsureDemoLoginUsersAsync(TravelAppDbContext dbContext)
    {
        var demoUsers = new[]
        {
            new { Email = "demo@example.com", UserName = "demo_user", Password = "Demo@123456", Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440001") },
            new { Email = "khanh@example.com", UserName = "khanh_user", Password = "Khanh@123456", Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440002") },
            new { Email = "guest@example.com", UserName = "guest_user", Password = "Guest@123456", Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440003") },
        };

        foreach (var demoUser in demoUsers)
        {
            var existingUser = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == demoUser.Email);
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(demoUser.Password);

            if (existingUser is null)
            {
                dbContext.Users.Add(new User
                {
                    Id = demoUser.Id,
                    UserName = demoUser.UserName,
                    Email = demoUser.Email,
                    PasswordHash = passwordHash,
                    IsActive = true,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
            }
            else
            {
                existingUser.UserName = demoUser.UserName;
                existingUser.PasswordHash = passwordHash;
                existingUser.IsActive = true;
            }
        }

        await dbContext.SaveChangesAsync();
    }
}
