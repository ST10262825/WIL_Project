using Microsoft.AspNetCore.Mvc;
using TutorConnect.WebApp.Models;
using TutorConnect.WebApp.Services;
using System.Linq;
using System.Threading.Tasks;

namespace TutorConnect.WebApp.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApiService _api;

        public AdminController(ApiService api)
        {
            _api = api;
        }

        public async Task<IActionResult> Index()
        {
            var tutors = await _api.GetTutorsAsync();
            return View(tutors);
        }

        public async Task<IActionResult> CreateTutor()
        {
            var modules = await _api.GetModulesAsync();
            ViewBag.Modules = modules;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateTutor(CreateTutorDTO dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Modules = await _api.GetModulesAsync();
                return View(dto);
            }

            try
            {
                await _api.CreateTutorAsync(dto);
                TempData["Success"] = "Tutor created successfully.";
                return RedirectToAction("Index");
            }
            catch (HttpRequestException ex)
            {
                ViewBag.Modules = await _api.GetModulesAsync();
                ViewBag.Error = ex.Message;
                return View(dto);
            }
        }

        public async Task<IActionResult> EditTutor(int id)
        {
            var tutor = await _api.GetTutorByIdAsync(id);
            var modules = await _api.GetModulesAsync();
            ViewBag.Modules = modules;

            var updateDto = new UpdateTutorDTO
            {
                Id = tutor.Id,
                Name = tutor.Name,
                Surname = tutor.Surname,
                Phone = tutor.Phone,
                Bio = tutor.Bio,
                IsBlocked = tutor.IsBlocked,
                ModuleIds = tutor.Modules.Select(m => m.Id).ToList()
            };

            return View(updateDto);
        }

        [HttpPost]
        public async Task<IActionResult> EditTutor(UpdateTutorDTO dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Modules = await _api.GetModulesAsync();
                return View(dto);
            }

            try
            {
                await _api.UpdateTutorAsync(dto);
                TempData["Success"] = "Tutor updated successfully.";
                return RedirectToAction("Index");
            }
            catch (HttpRequestException ex)
            {
                ViewBag.Modules = await _api.GetModulesAsync();
                ViewBag.Error = ex.Message;
                return View(dto);
            }
        }

        public async Task<IActionResult> DeleteTutor(int id)
        {
            await _api.DeleteTutorAsync(id);
            TempData["Success"] = "Tutor deleted successfully.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> ToggleBlockTutor(int id)
        {
            await _api.ToggleBlockTutorAsync(id);
            return RedirectToAction("Index");
        }
    }
}
