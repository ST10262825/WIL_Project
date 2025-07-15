using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TutorConnect.WebApp.Models;
using TutorConnect.WebApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Collections.Generic;
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

        public IActionResult Login() => View();
        public IActionResult Register() => View();
        public IActionResult Verify(string email = "")
        {
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDTO loginDto)
        {
            try
            {
                var token = await _apiService.LoginAsync(loginDto);

                if (token == null)
                {
                    ViewBag.Error = "Login failed. Please try again.";
                    return View();
                }

                // normal login flow
                HttpContext.Session.SetString("AuthToken", token);

                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var role = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                var studentId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                var claims = new List<Claim>
{
    new Claim(ClaimTypes.Name, loginDto.Email),
    new Claim("Token", token)
};

                if (!string.IsNullOrEmpty(role))
                    claims.Add(new Claim(ClaimTypes.Role, role));

                if (!string.IsNullOrEmpty(studentId))
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, studentId));


                var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
                await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Index", "Home");
            }
            catch (HttpRequestException ex)
            {
                if (ex.Message.Contains("Email not verified"))
                {
                    // Redirect to verify view
                    TempData["UnverifiedEmail"] = loginDto.Email;
                    return RedirectToAction("Verify");
                }

                ViewBag.Error = ex.Message;
                return View();
            }
        }


        [HttpGet]
        public IActionResult Verify()
        {
            var email = TempData["UnverifiedEmail"] as string;
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login");

            ViewBag.Email = email;
            return View();
        }

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


        [HttpPost]
        public async Task<IActionResult> Register(RegisterStudentDTO dto)
        {
            try
            {
                var success = await _apiService.RegisterStudentAsync(dto);
                if (!success)
                {
                    ViewBag.Error = "Registration failed.";
                    return View();
                }

                TempData["SuccessMessage"] = "Registration successful. Please check your email and enter your verification token.";
                return RedirectToAction("Verify", new { email = dto.Email });
            }
            catch (HttpRequestException ex)
            {
                ViewBag.Error = ex.Message;
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("Cookies");
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }


    }
}
