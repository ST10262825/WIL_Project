using Microsoft.AspNetCore.Mvc;
using TutorConnect.WebApp.Services;

namespace TutorConnect.WebApp.Controllers
{
    public class ThemeController : Controller
    {
        private readonly ApiService _apiService;

        public ThemeController(ApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTheme([FromBody] ThemeUpdateModel model)
        {
            try
            {
                Console.WriteLine($"[ThemeController] UpdateTheme called with: {model?.Theme}");

                if (model == null || string.IsNullOrEmpty(model.Theme))
                    return BadRequest("Theme is required");

                if (model.Theme != "light" && model.Theme != "dark")
                    return BadRequest("Invalid theme value");

                var success = await _apiService.UpdateThemePreferenceAsync(model.Theme);

                if (success)
                {
                    Console.WriteLine($"[ThemeController] Theme updated successfully: {model.Theme}");
                    return Ok(new { message = "Theme updated successfully", theme = model.Theme });
                }
                else
                {
                    Console.WriteLine($"[ThemeController] Failed to update theme via ApiService");
                    return BadRequest("Failed to update theme");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ThemeController] Error: {ex.Message}");
                return StatusCode(500, $"Error updating theme: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTheme()
        {
            try
            {
                Console.WriteLine($"[ThemeController] GetTheme called");
                var theme = await _apiService.GetThemePreferenceAsync();
                Console.WriteLine($"[ThemeController] Retrieved theme: {theme}");
                return Ok(new { theme = theme });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ThemeController] GetTheme error: {ex.Message}");
                // If API call fails, fall back to light theme
                return Ok(new { theme = "light" });
            }
        }
    }

    public class ThemeUpdateModel
    {
        public string Theme { get; set; }
    }
}