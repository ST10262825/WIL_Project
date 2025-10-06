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



        // Dashboard with comprehensive stats
        public async Task<IActionResult> Index()
        {
            try
            {
                var tutors = await _api.GetAllAdminTutorsAsync();
                var students = await _api.GetAllStudentsAsync();
                var bookings = await _api.GetAllBookingsAsync();
                var modules = await _api.GetAllModulesAsync();

                // Calculate comprehensive statistics

                var activeTutors = tutors?.Count(t => !t.IsBlocked) ?? 0;
                var blockedTutors = tutors?.Count(t => t.IsBlocked) ?? 0;
                var activeStudents = students?.Count(s => !s.IsBlocked) ?? 0;
                var blockedStudents = students?.Count(s => s.IsBlocked) ?? 0;

                // Recent activity (last 7 days)
                var lastWeek = DateTime.UtcNow.AddDays(-7);
                var recentBookings = bookings?.Count(b => b.StartTime >= lastWeek) ?? 0;
                var completedThisWeek = bookings?.Count(b => b.Status == "Completed" && b.StartTime >= lastWeek) ?? 0;

                // Calculate booking completion rate
                var bookingCompletionRate = bookings?.Any() == true ?
                    (decimal)bookings.Count(b => b.Status == "Completed") / bookings.Count * 100 : 0;

                // Calculate average tutor rating
                var averageTutorRating = tutors?.Where(t => t.AverageRating > 0).Average(t => t.AverageRating) ?? 0;

                var viewModel = new AdminDashboardViewModel
                {
                    // Basic counts
                    TotalTutors = tutors?.Count ?? 0,
                    TotalStudents = students?.Count ?? 0,
                    TotalBookings = bookings?.Count ?? 0,
                    TotalModules = modules?.Count ?? 0,

                    // Status breakdowns
                    ActiveTutors = activeTutors,
                    BlockedTutors = blockedTutors,
                    ActiveStudents = activeStudents,
                    BlockedStudents = blockedStudents,

                    // Booking statuses
                    PendingBookings = bookings?.Count(b => b.Status == "Pending") ?? 0,
                    ConfirmedBookings = bookings?.Count(b => b.Status == "Confirmed") ?? 0,
                    CompletedBookings = bookings?.Count(b => b.Status == "Completed") ?? 0,
                    CancelledBookings = bookings?.Count(b => b.Status == "Cancelled") ?? 0,


                    // Recent activity
                    RecentBookings = recentBookings,
                    CompletedThisWeek = completedThisWeek,

                    // Performance metrics
                    AverageTutorRating = averageTutorRating,
                    BookingCompletionRate = bookingCompletionRate,

                    // Lists
                    PopularModules = modules?
                        .OrderByDescending(m => m.BookingCount)
                        .Take(5)
                        .ToList() ?? new List<ModuleDTO>(),
                    TopRatedTutors = tutors?
                        .Where(t => t.AverageRating > 0)
                        .OrderByDescending(t => t.AverageRating)
                        .Take(5)
                        .ToList() ?? new List<TutorDTO>(),
                    Students = students?.ToList() ?? new List<StudentDTO>(),
                    Bookings = bookings?.ToList() ?? new List<BookingDTO>(),
                    Modules = modules?.ToList() ?? new List<ModuleDTO>(),

                    // System health (mock data for now)
                    SystemHealth = new SystemHealthDTO
                    {
                        DatabaseStatus = "Online",
                        Uptime = 86400, // 24 hours in seconds
                        MemoryUsage = 512, // MB
                        ActiveConnections = 42
                    }
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error loading dashboard: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                TempData["Error"] = $"Error loading dashboard: {ex.Message}";

                // Return empty view model instead of crashing
                return View(new AdminDashboardViewModel());
            }
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


        public async Task<IActionResult> Reports()
        {
            try
            {
                var options = await _api.GetReportOptionsAsync();
                ViewBag.ReportOptions = options;

                // Default filters
                var defaultFilters = new ReportFilterDTO
                {
                    DateRange = "last7days",
                    ReportType = "Booking"
                };

                ViewBag.DefaultFilters = defaultFilters;

                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading report options: {ex.Message}";
                return View();
            }
        }



        [HttpPost]
        public async Task<IActionResult> GenerateReport([FromForm] ReportFilterViewModel model)
        {
            // Add the [FromForm] attribute
            try
            {
                Console.WriteLine("=== WEBAPP CONTROLLER - FORM BINDING ===");
                Console.WriteLine($"Model is null: {model == null}");

                if (model != null)
                {
                    Console.WriteLine($"ReportType: '{model.ReportType}'");
                    Console.WriteLine($"DateRange: '{model.DateRange}'");
                    Console.WriteLine($"ExportFormat: '{model.ExportFormat}'");
                    Console.WriteLine($"UserType: '{model.UserType}'");
                    Console.WriteLine($"UserId: {model.UserId}");
                }
                else
                {
                    Console.WriteLine("MODEL IS NULL - CHECK FORM BINDING");
                }

                // Validate required fields
                if (string.IsNullOrEmpty(model?.ReportType))
                {
                    Console.WriteLine("ERROR: ReportType is null or empty");
                    return BadRequest("Please select a report type.");
                }

                Console.WriteLine("=== CREATING FILTERS DTO ===");

                // Convert ViewModel to DTO
                var filters = new ReportFilterDTO
                {
                    ReportType = model.ReportType,
                    DateRange = model.DateRange,
                    ExportFormat = model.ExportFormat,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    UserType = model.UserType,
                    UserId = model.UserId,
                    Statuses = new List<string>(),
                    ModuleIds = new List<int>()
                };

                Console.WriteLine("Initial filters created:");
                Console.WriteLine($"- ReportType: '{filters.ReportType}'");
                Console.WriteLine($"- UserType: '{filters.UserType}'");
                Console.WriteLine($"- UserId: {filters.UserId}");
                Console.WriteLine($"- Statuses count: {filters.Statuses.Count}");
                Console.WriteLine($"- ModuleIds count: {filters.ModuleIds.Count}");

                // SPECIAL HANDLING FOR "EVERYTHING" REPORT
                if (model.ReportType == "Everything")
                {
                    Console.WriteLine("Processing as 'Everything' report - clearing filters");
                    filters.UserType = null;
                    filters.UserId = null;
                    filters.Statuses = new List<string>(); // Ensure empty list
                    filters.ModuleIds = new List<int>();   // Ensure empty list

                    Console.WriteLine("After Everything report processing:");
                    Console.WriteLine($"- UserType: '{filters.UserType}'");
                    Console.WriteLine($"- UserId: {filters.UserId}");
                    Console.WriteLine($"- Statuses: [{string.Join(", ", filters.Statuses)}]");
                    Console.WriteLine($"- ModuleIds: [{string.Join(", ", filters.ModuleIds)}]");
                }
                else
                {
                    Console.WriteLine($"Processing as '{model.ReportType}' report");
                    // Add statuses from checkboxes
                    if (model.ReportType == "Booking")
                    {
                        if (model.StatusPending) filters.Statuses.Add("Pending");
                        if (model.StatusConfirmed) filters.Statuses.Add("Confirmed");
                        if (model.StatusCompleted) filters.Statuses.Add("Completed");
                        if (model.StatusCancelled) filters.Statuses.Add("Cancelled");
                    }
                    else if (model.ReportType == "Student" || model.ReportType == "Tutor")
                    {
                        if (model.StatusActive) filters.Statuses.Add("Active");
                        if (model.StatusInactive) filters.Statuses.Add("Inactive");
                    }

                    // Add module IDs
                    if (model.ModuleIds != null && model.ModuleIds.Any())
                    {
                        filters.ModuleIds = model.ModuleIds;
                    }
                }

                Console.WriteLine("=== FINAL FILTERS BEFORE API CALL ===");
                Console.WriteLine($"ReportType: '{filters.ReportType}'");
                Console.WriteLine($"DateRange: '{filters.DateRange}'");
                Console.WriteLine($"ExportFormat: '{filters.ExportFormat}'");
                Console.WriteLine($"UserType: '{filters.UserType}'");
                Console.WriteLine($"UserId: {filters.UserId}");
                Console.WriteLine($"StartDate: {filters.StartDate}");
                Console.WriteLine($"EndDate: {filters.EndDate}");
                Console.WriteLine($"Statuses: [{string.Join(", ", filters.Statuses)}] (Count: {filters.Statuses.Count})");
                Console.WriteLine($"ModuleIds: [{string.Join(", ", filters.ModuleIds)}] (Count: {filters.ModuleIds.Count})");
                Console.WriteLine("=== END WEBAPP DEBUG ===");

                // Check if this is a file export request
                if (!string.IsNullOrEmpty(filters.ExportFormat))
                {
                    Console.WriteLine($"Making FILE EXPORT API call for {filters.ExportFormat}");
                    try
                    {
                        var fileResult = await _api.GenerateReportFileAsync(filters);
                        Console.WriteLine("File export API call SUCCESS");
                        return File(fileResult.Content, fileResult.ContentType, fileResult.FileName);
                    }
                    catch (HttpRequestException ex)
                    {
                        Console.WriteLine($"FILE EXPORT API ERROR: {ex.Message}");
                        return BadRequest(new { error = ex.Message });
                    }
                }
                else
                {
                    Console.WriteLine("Making BROWSER VIEW API call");
                    try
                    {
                        var reportResult = await _api.GenerateReportAsync(filters);
                        Console.WriteLine("Browser view API call SUCCESS");
                        return PartialView("_ReportResults", reportResult);
                    }
                    catch (HttpRequestException ex)
                    {
                        Console.WriteLine($"BROWSER VIEW API ERROR: {ex.Message}");
                        return BadRequest(ex.Message);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTPREQUESTEXCEPTION CAUGHT: {ex.Message}");
                var errorMessage = ex.Message;

                if (!string.IsNullOrEmpty(model?.ExportFormat))
                {
                    return BadRequest(new { error = errorMessage });
                }
                return BadRequest(errorMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UNEXPECTED ERROR: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                var userFriendlyMessage = "An unexpected error occurred while generating the report. Please try again later.";

                if (!string.IsNullOrEmpty(model?.ExportFormat))
                {
                    return BadRequest(new { error = userFriendlyMessage });
                }
                return BadRequest(userFriendlyMessage);
            }
        }



        private string GetContentType(string exportFormat)
        {
            return exportFormat?.ToLower() switch
            {
                "pdf" => "application/pdf",
                "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "csv" => "text/csv",
                _ => "application/octet-stream"
            };
        }

    }
}