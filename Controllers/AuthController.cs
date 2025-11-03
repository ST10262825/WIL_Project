using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Security.Cryptography;
using TutorConnectAPI.Data;
using TutorConnectAPI.DTOs;
using TutorConnectAPI.Models;
using TutorConnectAPI.Services;

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


        [HttpPost("update-theme")]
        public async Task<IActionResult> UpdateThemePreference([FromBody] ThemePreferenceDTO dto)
        {
            try
            {
                // Alternative approach: Get user by email from token
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    Console.WriteLine($"[DEBUG] No email claim found");
                    return Unauthorized("User not authenticated");
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                if (user == null)
                    return NotFound("User not found");

                // Validate theme value
                if (dto.Theme != "light" && dto.Theme != "dark")
                    return BadRequest("Invalid theme value. Must be 'light' or 'dark'");

                Console.WriteLine($"[API] Updating theme for user {user.UserId} ({user.Email}): {user.ThemePreference} -> {dto.Theme}");

                user.ThemePreference = dto.Theme;
                await _context.SaveChangesAsync();

                Console.WriteLine($"[API] Theme updated successfully for user {user.UserId}. New theme: {user.ThemePreference}");

                return Ok(new
                {
                    message = "Theme preference updated successfully",
                    theme = user.ThemePreference
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating theme preference");
                return StatusCode(500, "Error updating theme preference");
            }
        }

        [HttpGet("theme-preference")]
        public async Task<IActionResult> GetThemePreference()
        {
            try
            {
                // Alternative approach: Get user by email from token
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    Console.WriteLine($"[DEBUG] No email claim found in GetThemePreference");
                    return Unauthorized("User not authenticated");
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                if (user == null)
                    return NotFound("User not found");

                Console.WriteLine($"[API] Getting theme preference for user {user.UserId}: {user.ThemePreference}");

                return Ok(new { theme = user.ThemePreference });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting theme preference");
                return StatusCode(500, "Error getting theme preference");
            }
        }



        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO dto)
        {
            try
            {
                string normalizedEmail = dto.Email.Trim().ToLower();

                // Check if user exists
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

                if (user == null)
                {
                    // Don't reveal that the user doesn't exist for security
                    return Ok("If an account with that email exists, a password reset link has been sent.");
                }

                // Check if user is blocked
                if (user.IsBlocked)
                {
                    return BadRequest("Account is blocked. Please contact administrator.");
                }

                // Check if email is verified
                if (!user.IsEmailVerified)
                {
                    return BadRequest("Please verify your email before resetting password.");
                }

                // Generate reset token (6-digit code like your verification)
                string resetToken = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

                // Set token and expiration (15 minutes)
                user.PasswordResetToken = resetToken;
                user.ResetTokenExpires = DateTime.UtcNow.AddMinutes(15);

                await _context.SaveChangesAsync();

                // Send reset email
                var body = $"""
            <h2>Password Reset Request</h2>
            <p>You requested to reset your password for your TutorConnect account.</p>
            <p>Here is your reset code:</p>
            <h3 style="background: #f4f4f4; padding: 10px; border-radius: 5px; display: inline-block;">
                {resetToken}
            </h3>
            <p>This code will expire in 15 minutes.</p>
            <p>If you didn't request this, please ignore this email.</p>
            """;

                await _emailService.SendEmailAsync(normalizedEmail, "Reset Your TutorConnect Password", body);

                _logger.LogInformation("Password reset token sent to {Email}", normalizedEmail);

                return Ok("If an account with that email exists, a password reset code has been sent.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in forgot password for email {Email}", dto.Email);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO dto)
        {
            try
            {
                // Validate new password
                if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
                {
                    return BadRequest("Password must be at least 6 characters long.");
                }

                if (dto.NewPassword != dto.ConfirmPassword)
                {
                    return BadRequest("New password and confirmation do not match.");
                }

                // Find user by valid reset token
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.PasswordResetToken == dto.Token &&
                                             u.ResetTokenExpires > DateTime.UtcNow);

                if (user == null)
                {
                    return BadRequest("Invalid or expired reset token.");
                }

                // Update password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                user.PasswordResetToken = null;
                user.ResetTokenExpires = null;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Password reset successfully for user {UserId}", user.UserId);

                // Send confirmation email
                var body = $"""
            <h2>Password Changed Successfully</h2>
            <p>Your TutorConnect password has been successfully reset.</p>
            <p>If you did not make this change, please contact support immediately.</p>
            """;

                await _emailService.SendEmailAsync(user.Email, "Password Reset Successful", body);

                return Ok("Password has been reset successfully. You can now log in with your new password.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password with token {Token}", dto.Token);
                return StatusCode(500, "An error occurred while resetting your password.");
            }
        }

        [HttpPost("validate-reset-token")]
        public async Task<IActionResult> ValidateResetToken([FromBody] ValidateResetTokenDTO dto)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.PasswordResetToken == dto.Token &&
                                             u.ResetTokenExpires > DateTime.UtcNow);

                if (user == null)
                {
                    return BadRequest("Invalid or expired reset token.");
                }

                return Ok("Token is valid.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating reset token");
                return StatusCode(500, "Error validating token.");
            }
        }

    }
}