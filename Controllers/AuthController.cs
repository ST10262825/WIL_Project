using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using TutorConnect.WebApp.Services;
using TutorConnect.WebApp.Models;
using System.Net.Http;

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
        public IActionResult Register() => View();

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
                    TempData["SuccessMessage"] = "Email verified. You can now log in.";
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
                return View(dto); // stops here if passwords don’t match
            }

            try
            {
                await _apiService.RegisterStudentAsync(new RegisterStudentDTO
                {
                    Email = dto.Email,
                    Password = dto.Password,
                    Name = dto.Name,
                    Course = dto.Course
                });

                TempData["SuccessMessage"] = "Registration successful. Please verify your email.";
                return RedirectToAction("Verify", new { email = dto.Email });
            }
            catch (HttpRequestException ex)
            {
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
            await HttpContext.SignOutAsync("Cookies");
       
            Response.Cookies.Delete(".AspNetCore.Cookies");
            return RedirectToAction("Login");
        }


    }
}
