using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using TutorConnect.WebApp.Models;
using TutorConnect.WebApp.Services;
using static TutorConnect.WebApp.Services.ApiService;

namespace TutorConnect.WebApp.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApiService _api;

        public AdminController(ApiService api)
        {
            _api = api;
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
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                              .Select(e => e.ErrorMessage)
                                              .ToList();
                ViewBag.Errors = errors;
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
            try
            {
                var tutor = await _api.GetAdminTutorByIdAsync(id);
                if (tutor == null)
                {
                    TempData["Error"] = "Tutor not found";
                    return RedirectToAction("Tutors");
                }

                var modules = await _api.GetModulesAsync();
                ViewBag.Modules = modules;

                var adminupdateDto = new AdminUpdateTutorDTO
                {
                    Id = tutor.TutorId,
                    Name = tutor.Name,
                    Surname = tutor.Surname,
                    Phone = tutor.Phone,
                    Email = tutor.Email, // Make sure your TutorDTO has Email property
                    Bio = tutor.Bio,
                    AboutMe = tutor.AboutMe,
                    Expertise = tutor.Expertise,
                    Education = tutor.Education,
                    IsBlocked = tutor.IsBlocked,
                    ModuleIds = tutor.Modules?.Select(m => m.ModuleId).ToList() ?? new List<int>()
                };

                return View(adminupdateDto);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading tutor: {ex.Message}";
                return RedirectToAction("Tutors");
            }
        }



        [HttpPost]
        public async Task<IActionResult> EditTutor(AdminUpdateTutorDTO dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Modules = await _api.GetModulesAsync();
                return View(dto);
            }

            try
            {
                var success = await _api.AdminUpdateTutorAsync(dto);
                if (success)
                {
                    TempData["Success"] = "Tutor updated successfully.";
                    return RedirectToAction("Tutors");
                }
                else
                {
                    ViewBag.Modules = await _api.GetModulesAsync();
                    ViewBag.Error = "Failed to update tutor. Please try again.";
                    return View(dto);
                }
            }
            catch (HttpRequestException ex)
            {
                ViewBag.Modules = await _api.GetModulesAsync();
                ViewBag.Error = ex.Message;
                return View(dto);
            }
        }


        [HttpPost]
        public async Task<IActionResult> DeleteTutor(int id)
        {
            try
            {
                Console.WriteLine($"=== DELETE TUTOR DEBUG ===");
                Console.WriteLine($"Tutor ID: {id}");
                Console.WriteLine($"TempData before: Success='{TempData["Success"]}', Error='{TempData["Error"]}'");

                // Call the API service
                var (success, message, hasPendingBookings) = await _api.DeleteTutorAsync(id);

                Console.WriteLine($"API Response: Success={success}, Message='{message}', HasPendingBookings={hasPendingBookings}");

                if (success)
                {
                    TempData["Success"] = message;
                    Console.WriteLine($"Set Success: {message}");
                }
                else
                {
                    TempData["Error"] = message;
                    Console.WriteLine($"Set Error: {message}");

                    // Also store debug info
                    TempData["DebugInfo"] = $"Delete failed: {message} (Tutor ID: {id})";
                }

                Console.WriteLine($"TempData after: Success='{TempData["Success"]}', Error='{TempData["Error"]}'");
                Console.WriteLine($"=== END DEBUG ===");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION in DeleteTutor: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                TempData["Error"] = $"An unexpected error occurred: {ex.Message}";
                TempData["DebugInfo"] = $"Exception: {ex.Message}";
            }

            return RedirectToAction(nameof(Tutors));
        }



        // Dashboard
        public async Task<IActionResult> Index()
        {
            var tutors = await _api.GetAllAdminTutorsAsync();
            var students = await _api.GetAllStudentsAsync();
            var bookings = await _api.GetAllBookingsAsync();
            var modules = await _api.GetAllModulesAsync();

            ViewBag.Stats = new
            {
                TotalTutors = tutors?.Count ?? 0,
                TotalStudents = students?.Count ?? 0,
                TotalBookings = bookings?.Count ?? 0,
                PendingBookings = bookings?.Count(b => b.Status == "Pending") ?? 0,
                ActiveModules = modules?.Count ?? 0
            };

            return View(); // Admin Dashboard view you already have
        }

        // ------------------ Tutors ------------------
        public async Task<IActionResult> Tutors()
        {
            try
            {
                var tutors = await _api.GetAllAdminTutorsAsync();

                if (tutors == null)
                {
                    TempData["Error"] = "Failed to load tutors. Please try again.";
                    tutors = new List<TutorDTO>();
                }

                return View(tutors);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading tutors: {ex.Message}";
                return View(new List<TutorDTO>());
            }
        }



        [HttpPost]
        public async Task<IActionResult> BlockTutor(int id)
        {
            await _api.BlockTutorAsync(id);
            return RedirectToAction(nameof(Tutors));
        }

        [HttpPost]
        public async Task<IActionResult> UnblockTutor(int id)
        {
            await _api.UnblockTutorAsync(id);
            return RedirectToAction(nameof(Tutors));
        }



        // ------------------ Students ------------------
        public async Task<IActionResult> Students()
        {
            var students = await _api.GetAllStudentsAsync();
            return View(students);
        }

        // In your AdminController.cs

        [HttpPost]
        public async Task<IActionResult> BlockStudent(int id)
        {
            try
            {
                var success = await _api.BlockStudentAsync(id);
                if (success)
                {
                    TempData["Success"] = "Student blocked successfully.";
                }
                else
                {
                    TempData["Error"] = "Failed to block student.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error blocking student: {ex.Message}";
            }
            return RedirectToAction(nameof(Students));
        }

        [HttpPost]
        public async Task<IActionResult> UnblockStudent(int id)
        {
            try
            {
                var success = await _api.UnblockStudentAsync(id);
                if (success)
                {
                    TempData["Success"] = "Student unblocked successfully.";
                }
                else
                {
                    TempData["Error"] = "Failed to unblock student.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error unblocking student: {ex.Message}";
            }
            return RedirectToAction(nameof(Students));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            try
            {
                Console.WriteLine($"WebApp: Attempting to delete student ID: {id}");

                var (success, message) = await _api.DeleteStudentAsync(id);

                if (success)
                {
                    TempData["Success"] = message;
                }
                else
                {
                    TempData["Error"] = message;

                    // Log the specific error for debugging
                    Console.WriteLine($"Delete failed: {message}");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Unexpected error: {ex.Message}";
                Console.WriteLine($"Unexpected error in DeleteStudent: {ex.Message}");
            }

            return RedirectToAction(nameof(Students));
        }

        // ------------------ Bookings ------------------
        // In your AdminController.cs

        public async Task<IActionResult> Bookings(string status = "all")
        {
            try
            {
                var bookings = await _api.GetAllBookingsAsync();

                if (bookings == null)
                {
                    bookings = new List<BookingDTO>();
                }

                // Apply status filter
                if (!string.IsNullOrEmpty(status) && status != "all")
                {
                    bookings = bookings.Where(b => b.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                ViewBag.StatusFilter = status;
                return View(bookings);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading bookings: {ex.Message}";
                ViewBag.StatusFilter = status;
                return View(new List<BookingDTO>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            try
            {
                var success = await _api.DeleteBookingAsync(id);

                if (success)
                {
                    TempData["Success"] = "Booking deleted successfully.";
                }
                else
                {
                    TempData["Error"] = "Cannot delete booking. Only completed, cancelled, or declined bookings can be deleted.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting booking: {ex.Message}";
            }

            return RedirectToAction(nameof(Bookings));
        }

        // ------------------ Modules ------------------
        // In your AdminController.cs

        public async Task<IActionResult> Modules()
        {
            try
            {
                var modules = await _api.GetAllModulesAsync();
                return View(modules ?? new List<ModuleDTO>());
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading modules: {ex.Message}";
                return View(new List<ModuleDTO>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateModule(string Code, string Name)
        {
            try
            {
                if (string.IsNullOrEmpty(Code) || string.IsNullOrEmpty(Name))
                {
                    TempData["Error"] = "Module code and name are required.";
                    return RedirectToAction(nameof(Modules));
                }

                var request = new CreateModuleRequest { Code = Code.Trim(), Name = Name.Trim() };
                var (success, message) = await _api.CreateModuleAsync(request);

                if (success)
                {
                    TempData["Success"] = message;
                }
                else
                {
                    TempData["Error"] = message;
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating module: {ex.Message}";
            }

            return RedirectToAction(nameof(Modules));
        }



        [HttpPost]
        public async Task<IActionResult> EditModule(int id, string Code, string Name)
        {
            try
            {
                if (string.IsNullOrEmpty(Code) || string.IsNullOrEmpty(Name))
                {
                    TempData["Error"] = "Module code and name are required.";
                    return RedirectToAction(nameof(Modules));
                }

                var request = new UpdateModuleRequest { Code = Code.Trim(), Name = Name.Trim() };
                var (success, message) = await _api.UpdateModuleAsync(id, request);

                if (success)
                {
                    TempData["Success"] = message;
                }
                else
                {
                    TempData["Error"] = message;
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating module: {ex.Message}";
            }

            return RedirectToAction(nameof(Modules));
        }



        [HttpPost]
        public async Task<IActionResult> DeleteModule(int id)
        {
            try
            {
                var (success, message) = await _api.DeleteModuleAsync(id);

                if (success)
                {
                    TempData["Success"] = message;
                }
                else
                {
                    TempData["Error"] = message;
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting module: {ex.Message}";
            }

            return RedirectToAction(nameof(Modules));
        }




    }
}