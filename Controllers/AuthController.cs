using Microsoft.AspNetCore.Authorization;
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
        private const string CURRENT_POPIA_VERSION = "1.0";


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
            try
            {
                // POPIA Compliance Check - REQUIRED for registration
                if (!dto.HasAcceptedPOPIA)
                {
                    return BadRequest("You must accept the POPIA terms and conditions to register.");
                }

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

                // 3. Validate password strength
                if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
                    return BadRequest("Password must be at least 6 characters long.");

                // 4. Create user with POPIA data
                string verificationToken = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

                var user = new User
                {
                    Email = normalizedEmail,
                    Role = "Student",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    IsEmailVerified = false,
                    VerificationToken = verificationToken,

                    // POPIA Compliance Data
                    HasAcceptedPOPIA = dto.HasAcceptedPOPIA,
                    POPIAAcceptedDate = DateTime.UtcNow,
                    POPIAVersion = CURRENT_POPIA_VERSION,
                    MarketingConsent = dto.MarketingConsent,
                    LastConsentUpdate = DateTime.UtcNow
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

                // Log POPIA consent for audit trail
                _logger.LogInformation("POPIA consent recorded for user {UserId}. Version: {Version}, Marketing: {Marketing}",
                    user.UserId, CURRENT_POPIA_VERSION, dto.MarketingConsent);

                // Non-blocking gamification
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

                // Updated email with POPIA information
                var body = $"""
                <h2>Welcome to TutorConnect!</h2>
                <p>Here is your verification code:</p>
                <h3>{verificationToken}</h3>
                <p>Please copy and paste it into the verification form on the website.</p>
                
                <hr>
                <div style="background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin-top: 20px;">
                    <h4>POPIA Compliance Information</h4>
                    <p><strong>Privacy Policy Accepted:</strong> {dto.HasAcceptedPOPIA}</p>
                    <p><strong>Marketing Communications:</strong> {(dto.MarketingConsent ? "Yes" : "No")}</p>
                    <p><strong>Acceptance Date:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm}</p>
                    <p><small>You can update your communication preferences in your account settings at any time.</small></p>
                </div>
                """;

                await _emailService.SendEmailAsync(normalizedEmail, "Verify your TutorConnect account", body);

                return Ok(new
                {
                    message = "Student registered successfully. Please check your email to verify your account.",
                    popiaAccepted = true,
                    marketingConsent = dto.MarketingConsent
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during student registration");
                return StatusCode(500, "An error occurred during registration.");
            }
        }

        // NEW: POPIA Consent Management Endpoints

        [HttpPost("update-consent")]
        [Authorize]
        public async Task<IActionResult> UpdateConsent([FromBody] UpdateConsentDTO dto)
        {
            try
            {
                var userId = GetUserIdFromToken();
                if (userId == 0)
                    return Unauthorized("User not authenticated");

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return NotFound("User not found");

                // Update consent preferences
                user.MarketingConsent = dto.MarketingConsent;
                user.LastConsentUpdate = DateTime.UtcNow;

                // If POPIA terms were updated, require re-acceptance
                if (dto.HasAcceptedPOPIA && user.POPIAVersion != CURRENT_POPIA_VERSION)
                {
                    user.HasAcceptedPOPIA = true;
                    user.POPIAAcceptedDate = DateTime.UtcNow;
                    user.POPIAVersion = CURRENT_POPIA_VERSION;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Consent updated for user {UserId}. Marketing: {Marketing}, POPIA Version: {Version}",
                    userId, dto.MarketingConsent, user.POPIAVersion);

                return Ok(new
                {
                    message = "Consent preferences updated successfully",
                    marketingConsent = user.MarketingConsent,
                    popiaVersion = user.POPIAVersion,
                    lastUpdated = user.LastConsentUpdate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating consent for user");
                return StatusCode(500, "Error updating consent preferences");
            }
        }

        [HttpGet("consent-status")]
        [Authorize]
        public async Task<IActionResult> GetConsentStatus()
        {
            try
            {
                var userId = GetUserIdFromToken();
                if (userId == 0)
                    return Unauthorized("User not authenticated");

                var user = await _context.Users
                    .Where(u => u.UserId == userId)
                    .Select(u => new
                    {
                        u.HasAcceptedPOPIA,
                        u.POPIAAcceptedDate,
                        u.POPIAVersion,
                        u.MarketingConsent,
                        u.LastConsentUpdate,
                        CurrentPOPIAVersion = CURRENT_POPIA_VERSION,
                        NeedsReconsent = u.POPIAVersion != CURRENT_POPIA_VERSION
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                    return NotFound("User not found");

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting consent status for user");
                return StatusCode(500, "Error retrieving consent status");
            }
        }

        [HttpPost("request-data-export")]
        [Authorize]
        public async Task<IActionResult> RequestDataExport()
        {
            try
            {
                var userId = GetUserIdFromToken();
                if (userId == 0)
                    return Unauthorized("User not authenticated");

                // In a real implementation, this would trigger a background job
                // to compile all user data and send it to them
                _logger.LogInformation("Data export requested for user {UserId}", userId);

                // For now, return a confirmation
                return Ok(new
                {
                    message = "Your data export request has been received. You will receive an email with your data within 7 working days.",
                    requestId = Guid.NewGuid().ToString(),
                    estimatedCompletion = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing data export request");
                return StatusCode(500, "Error processing data export request");
            }
        }

        [HttpPost("request-account-deletion")]
        [Authorize]
        public async Task<IActionResult> RequestAccountDeletion([FromBody] DeletionRequestDTO dto)
        {
            try
            {
                var userId = GetUserIdFromToken();
                if (userId == 0)
                    return Unauthorized("User not authenticated");

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return NotFound("User not found");

                // Log the deletion request (in production, this would go to a queue)
                _logger.LogWarning("Account deletion requested for user {UserId}. Reason: {Reason}",
                    userId, dto.Reason);

                // In a real implementation, you would:
                // 1. Schedule the deletion for 30 days in the future (grace period)
                // 2. Send confirmation email
                // 3. Anonymize data according to POPIA requirements

                return Ok(new
                {
                    message = "Your account deletion request has been received. Your account will be permanently deleted after 30 days. You may cancel this request within that period.",
                    requestId = Guid.NewGuid().ToString(),
                    scheduledDeletion = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd"),
                    cancellationDeadline = DateTime.UtcNow.AddDays(29).ToString("yyyy-MM-dd")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing account deletion request");
                return StatusCode(500, "Error processing account deletion request");
            }
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