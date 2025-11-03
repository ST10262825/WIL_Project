using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using TutorConnect.WebApp.Models;
using TutorConnect.WebApp.Services;

namespace TutorConnect.WebApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApiService _apiService;

        public AuthController(ApiService apiService)
        {
            _apiService = apiService;
        }

        // =============================
        // Views
        // =============================
        [HttpGet]
        public IActionResult Login() => View();

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            try
            {
                // Get available courses for dropdown
                var courses = await _apiService.GetCoursesAsync();
                ViewBag.Courses = new SelectList(courses ?? new List<CourseDTO>(), "CourseId", "Title");

                return View();
            }
            catch (Exception ex)
            {
                // If courses fail to load, still show the form but log the error
                Console.WriteLine($"Error loading courses: {ex.Message}");
                ViewBag.Courses = new SelectList(new List<CourseDTO>(), "CourseId", "Title");
                return View();
            }
        }

        [HttpGet]
        public IActionResult Verify(string? email)
        {
            var resolvedEmail = email ?? TempData["UnverifiedEmail"] as string;
            if (string.IsNullOrEmpty(resolvedEmail))
                return RedirectToAction("Login");

            ViewBag.Email = resolvedEmail;
            return View();
        }

        // =============================
        // Login
        // =============================
        [HttpPost]
        public async Task<IActionResult> Login(LoginDTO loginDto)
        {
            if (!ModelState.IsValid)
                return View(loginDto);

            try
            {
                var token = await _apiService.LoginAsync(loginDto);
                if (token == null)
                {
                    ViewBag.Error = "Login failed. Please try again.";
                    return View(loginDto);
                }

                // Parse JWT
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    ViewBag.Error = "Session expired. Please log in again.";
                    return View(loginDto);
                }

                var role = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                // Claims for cookie
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, loginDto.Email),
            new Claim(ClaimTypes.NameIdentifier, userId ?? ""),
            new Claim("Token", token),
            new Claim("JWT_Expires", jwtToken.ValidTo.ToString("o"))
        };

                if (!string.IsNullOrEmpty(role))
                    claims.Add(new Claim(ClaimTypes.Role, role));

                var claimsIdentity = new ClaimsIdentity(claims, "Cookies");

                // Authentication properties
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = loginDto.RememberMe
                };

                if (loginDto.RememberMe)
                {
                    authProperties.ExpiresUtc = DateTime.UtcNow.AddDays(7);
                }

                await HttpContext.SignInAsync(
                    "Cookies",
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties
                );

                return RedirectToAction("Index", "Home");
            }
            catch (HttpRequestException ex)
            {
                // Handle blocked account message
                if (ex.Message.Contains("Account currently blocked", StringComparison.OrdinalIgnoreCase))
                {
                    ViewBag.Error = "Account currently blocked. Please contact your Administrator.";
                }
                else if (ex.Message.Contains("Email not verified"))
                {
                    TempData["UnverifiedEmail"] = loginDto.Email;
                    return RedirectToAction("Verify");
                }
                else
                {
                    ViewBag.Error = ex.Message;
                }

                return View(loginDto);
            }
        }





        // =============================
        // Email Verification
        // =============================
        [HttpPost]
        public async Task<IActionResult> Verify(string email, string token)
        {
            try
            {
                var success = await _apiService.VerifyEmailAsync(token);
                if (success)
                {
                    // NEW: Enhanced verification success message
                    TempData["SuccessMessage"] = "🎉 Email verified successfully! You've earned 50 points. You can now log in and start earning more points!";
                    return RedirectToAction("Login");
                }

                ViewBag.Error = "Verification failed.";
                ViewBag.Email = email;
                return View();
            }
            catch (HttpRequestException ex)
            {
                ViewBag.Error = ex.Message;
                ViewBag.Email = email;
                return View();
            }
        }

        // =============================
        // Register
        // =============================
        [HttpPost]
        public async Task<IActionResult> Register(RegisterStudentDTO dto)
        {
            if (!ModelState.IsValid)
            {
                // Reload courses on validation error
                var courses = await _apiService.GetCoursesAsync();
                ViewBag.Courses = new SelectList(courses ?? new List<CourseDTO>(), "CourseId", "Title");
                return View(dto);
            }

            try
            {
                await _apiService.RegisterStudentAsync(new RegisterStudentDTO
                {
                    Email = dto.Email,
                    Password = dto.Password,
                    Name = dto.Name,
                    CourseId = dto.CourseId // CHANGE FROM Course TO CourseId
                });

                // NEW: Enhanced success message with gamification benefits
                TempData["SuccessMessage"] = "Registration successful! 🎉 You've earned 100 welcome points! Please verify your email to earn 50 more points.";
                return RedirectToAction("Verify", new { email = dto.Email });
            }
            catch (HttpRequestException ex)
            {
                // Reload courses on API error
                var courses = await _apiService.GetCoursesAsync();
                ViewBag.Courses = new SelectList(courses ?? new List<CourseDTO>(), "CourseId", "Title");
                ViewBag.Error = ex.Message;
                return View(dto);
            }
        }


        // =============================
        // Logout
        // =============================
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            try
            {
                Console.WriteLine("[AuthController] Logout - Resetting theme to light");

                // Set a temporary cookie to force light theme
                Response.Cookies.Append("force_theme", "light", new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddMinutes(5), // Short expiration
                    HttpOnly = false,
                    IsEssential = true,
                    Path = "/"
                });

                // Your existing logout logic
                await HttpContext.SignOutAsync("Cookies");
                Response.Cookies.Delete(".AspNetCore.Cookies");

                Console.WriteLine("[AuthController] Logout completed");

                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AuthController] Logout error: {ex.Message}");
                return RedirectToAction("Login");
            }
        }


        // GET: /Account/ForgotPassword
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDTO model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _apiService.ForgotPasswordAsync(model.Email);
                TempData["SuccessMessage"] = "If an account with that email exists, a password reset code has been sent.";
                return RedirectToAction("ResetPassword");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        // GET: /Auth/ResetPassword
        public IActionResult ResetPassword()
        {
            return View();
        }

        // POST: /Auth/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordDTO model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _apiService.ResetPasswordAsync(model.Token, model.NewPassword, model.ConfirmPassword);
                TempData["SuccessMessage"] = "Password has been reset successfully! You can now log in.";
                return RedirectToAction("Login");
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

    }
}
