using TutorConnectAPI.Data;
using TutorConnectAPI.Models;
using BCrypt.Net;

public static class DbSeeder
{
    public static void Seed(ApplicationDbContext context)
    {
        if (!context.Users.Any(u => u.Role == "Admin"))
        {
            var adminUser = new User
            {
                Email = "admin@tutorconnect.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("AdminPassword123"), 
                Role = "Admin",
                IsActive = true,
                IsEmailVerified = true, 
                VerificationToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjMwMDgiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9lbWFpbGFkZHJlc3MiOiJhZG1pbkB0dXRvcmNvbm5lY3QuY29tIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjoiQWRtaW4iLCJleHAiOjE3NDg3MDE4Mjh9.Y34wkdqOM62kSjV101p2ai0fG6afBExMjPnu0WN3uy4"
            };

            context.Users.Add(adminUser);
            context.SaveChanges();
        }
    }
}
