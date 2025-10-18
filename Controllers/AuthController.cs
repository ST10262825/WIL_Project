using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using TutorConnectAPI.Data;
using TutorConnectAPI.DTOs;
using TutorConnectAPI.Models;
using TutorConnectAPI.Services;
using Microsoft.Extensions.Logging;

namespace TutorConnectAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly TokenService _tokenService;
        private readonly EmailService _emailService;
        private readonly IGamificationService _gamificationService;
        private readonly ILogger<AuthController> _logger;


        // Update constructor to include gamification service
        public AuthController(ApplicationDbContext context, TokenService tokenService, EmailService emailService, IGamificationService gamificationService, ILogger<AuthController> logger)
        {
            _context = context;
            _tokenService = tokenService;
            _emailService = emailService;
            _gamificationService = gamificationService;
            _logger = logger;

        }

        [HttpPost("student-register")]
        public async Task<IActionResult> RegisterStudent(RegisterStudentDTO dto)
        {
            var courseExists = await _context.Courses.AnyAsync(c => c.CourseId == dto.CourseId);
            if (!courseExists)
                return BadRequest("Selected course does not exist");

            string normalizedEmail = dto.Email.Trim().ToLower();

            // 1. Validate email domain
            if (!normalizedEmail.EndsWith("@vcconnect.edu.za"))
                return BadRequest("Only @vcconnect.edu.za email addresses are allowed.");

            // 2. Check if user exists
            var existingUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (existingUser != null)
                return BadRequest("An account with this email already exists.");

            // 3. Validate password strength (optional rule, you can modify)
            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
                return BadRequest("Password must be at least 6 characters long.");

            // 4. Create user and student
            string verificationToken = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

            var user = new User
            {
                Email = normalizedEmail,
                Role = "Student",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                IsEmailVerified = false,
                VerificationToken = verificationToken
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var student = new Student
            {
                UserId = user.UserId,
                Name = dto.Name,
                CourseId = dto.CourseId
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            // ✅ FIXED: Non-blocking gamification
            _ = Task.Run(async () =>
            {
                try
                {
                    await _gamificationService.AwardPointsAsync(
                        user.UserId,
                        "AccountCreated",
                        100,
                        "Welcome to TutorConnect!"
                    );
                    await _gamificationService.CheckAndAwardAchievementsAsync(user.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background gamification error during registration for user {UserId}", user.UserId);
                }
            });

            var verifyUrl = $"https://localhost:44374/api/auth/verify-email?token={verificationToken}";
            var body = $"""
             <h2>Welcome to TutorConnect!</h2>
             <p>Here is your verification code:</p>
             <h3>{verificationToken}</h3>
            <p>Please copy and paste it into the verification form on the website.</p>
            """;

            await _emailService.SendEmailAsync(normalizedEmail, "Verify your TutorConnect account", body);

            return Ok("Student registered successfully. Please check your email to verify your account.");
        }



        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail(string token)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.VerificationToken == token);
            if (user == null)
                return BadRequest("Invalid or expired verification token.");

            user.IsEmailVerified = true;
            user.VerificationToken = null;

            await _context.SaveChangesAsync();

            //  Non-blocking gamification
            _ = Task.Run(async () =>
            {
                try
                {
                    await _gamificationService.AwardPointsAsync(
                        user.UserId,
                        "EmailVerified",
                        50,
                        "Email verified successfully"
                    );
                    await _gamificationService.CheckAndAwardAchievementsAsync(user.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background gamification error during email verification for user {UserId}", user.UserId);
                }
            });

            return Ok("Email verified successfully. You can now log in.");
        }



        [HttpPost("verify-token")]
        public async Task<IActionResult> VerifyToken([FromBody] VerifyTokenDTO dto)
        {
            var normalizedEmail = dto.Email.Trim().ToLower();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null)
                return BadRequest("No account found.");

            if (user.VerificationToken != dto.Token)
                return BadRequest("Invalid verification token.");

            user.IsEmailVerified = true;
            user.VerificationToken = null;

            await _context.SaveChangesAsync();

            //  Non-blocking gamification
            _ = Task.Run(async () =>
            {
                try
                {
                    await _gamificationService.AwardPointsAsync(
                        user.UserId,
                        "EmailVerified",
                        50,
                        "Email verified successfully"
                    );
                    await _gamificationService.CheckAndAwardAchievementsAsync(user.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background gamification error during token verification for user {UserId}", user.UserId);
                }
            });

            return Ok("Email verified successfully.");
        }




        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDTO dto)
        {
            string normalizedEmail = dto.Email.Trim().ToLower();

            // 1. Find user with related entities
            var user = await _context.Users
                .Include(u => u.Student)
                .Include(u => u.Tutor)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null)
                return Unauthorized("No account found with this email address.");

            // 2. Check if user is blocked using the helper property
            if (user.IsBlocked)
                return Unauthorized("Account currently blocked. Please contact your Administrator.");

            // 3. Check password
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized("Incorrect password.");

            // 4. Check email verification
            if (!user.IsEmailVerified)
                return Unauthorized(new { error = "Email not verified", email = user.Email });

            // 5. Check if user has the appropriate profile for their role
            if (user.Role == "Student" && user.Student == null)
                return Unauthorized("Student profile no longer exists.");

            if (user.Role == "Tutor" && user.Tutor == null)
                return Unauthorized("Tutor profile no longer exists.");

            //  Non-blocking gamification - don't await, just fire and forget
            _ = Task.Run(async () =>
            {
                try
                {
                    await _gamificationService.UpdateStreakAsync(user.UserId);

                    var profile = await _context.GamificationProfiles
                        .FirstOrDefaultAsync(p => p.UserId == user.UserId);

                    if (profile != null && profile.LastActivityDate.Date < DateTime.UtcNow.Date)
                    {
                        await _gamificationService.AwardPointsAsync(
                            user.UserId,
                            "DailyLogin",
                            10,
                            "Daily login bonus"
                        );
                    }
                    else if (profile == null)
                    {
                        await _gamificationService.AwardPointsAsync(
                            user.UserId,
                            "FirstLogin",
                            25,
                            "Welcome to TutorConnect!"
                        );
                    }

                    // Achievement check can happen in background
                    await _gamificationService.CheckAndAwardAchievementsAsync(user.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background gamification error for user {UserId}", user.UserId);
                }
            });

            var token = _tokenService.CreateToken(user);
            return Ok(new { token });
        }

        // NEW: Add endpoint to get user's gamification profile
        [HttpGet("gamification-profile")]
        public async Task<IActionResult> GetGamificationProfile()
        {
            try
            {
                // Get user ID from token (you'll need to extract this from JWT)
                var userId = GetUserIdFromToken();
                if (userId == 0)
                    return Unauthorized("User not authenticated");

                var profile = await _gamificationService.GetUserProfileAsync(userId);
                return Ok(profile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving gamification profile: {ex.Message}");
            }
        }

        // Helper method to extract user ID from JWT token
        private int GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (int.TryParse(userIdClaim, out int userId))
                return userId;
            return 0;
        }
    }
}