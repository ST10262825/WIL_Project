using Microsoft.AspNetCore.Mvc;
using TutorConnect.WebApp.Services;

namespace TutorConnect.WebApp.Controllers
{
   

    public class ModuleController : Controller
    {
        private readonly ApiService _apiService;

        public ModuleController(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            var modules = await _apiService.GetModulesAsync();
            return View(modules);
        }
    }

}
