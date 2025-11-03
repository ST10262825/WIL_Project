using Microsoft.JSInterop;

namespace TutorConnect.WebApp.Services
{
    public class ThemeService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ApiService _apiService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ThemeService(IJSRuntime jsRuntime, ApiService apiService, IHttpContextAccessor httpContextAccessor)
        {
            _jsRuntime = jsRuntime;
            _apiService = apiService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task InitializeAsync()
        {
            try
            {
                var isAuthenticated = _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
                string theme = "light";

                if (isAuthenticated)
                {
                    // Get theme from server for authenticated users
                    theme = await _apiService.GetThemePreferenceAsync();
                }
                else
                {
                    // For anonymous users, check cookie first (set during logout)
                    var themeCookie = _httpContextAccessor.HttpContext?.Request.Cookies["theme"];
                    if (themeCookie == "light")
                    {
                        theme = "light";
                    }
                    else
                    {
                        // Fall back to local storage
                        theme = await GetThemeFromLocalStorage();
                    }
                }

                await ApplyTheme(theme);
                Console.WriteLine($"[ThemeService] Initialized with theme: {theme}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ThemeService] Error initializing: {ex.Message}");
                await ApplyTheme("light");
            }
        }

        private async Task<string> GetThemeFromLocalStorage()
        {
            try
            {
                return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "theme") ?? "light";
            }
            catch
            {
                return "light";
            }
        }

        private async Task ApplyTheme(string theme)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("applyTheme", theme);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ThemeService] Error applying theme: {ex.Message}");
            }
        }
    }
}