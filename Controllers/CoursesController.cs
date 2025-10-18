using Microsoft.AspNetCore.Mvc;
using global::TutorConnect.WebApp.Models;
using global::TutorConnect.WebApp.Services;
using Microsoft.AspNetCore.Authorization;


namespace TutorConnect.WebApp.Controllers
{
   
   

    namespace TutorConnect.WebApp.Controllers
    {
        [Authorize(Roles = "Admin")]
        public class CoursesController : Controller
        {
            private readonly ApiService _apiService;
            private readonly ILogger<CoursesController> _logger;

            public CoursesController(ApiService apiService, ILogger<CoursesController> logger)
            {
                _apiService = apiService;
                _logger = logger;
            }

            // GET: /Courses
            public async Task<IActionResult> Index()
            {
                try
                {
                    var courses = await _apiService.GetCoursesAsync();
                    return View(courses ?? new List<CourseDTO>());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading courses");
                    TempData["Error"] = "Error loading courses. Please try again.";
                    return View(new List<CourseDTO>());
                }
            }

            // GET: /Courses/Create
            public IActionResult Create()
            {
                return View();
            }

            // POST: /Courses/Create
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Create(CreateCourseDTO model)
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                try
                {
                    var success = await _apiService.CreateCourseAsync(model);
                    if (success)
                    {
                        TempData["Success"] = "Course created successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["Error"] = "Failed to create course. Please try again.";
                        return View(model);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating course");
                    TempData["Error"] = "Error creating course. Please try again.";
                    return View(model);
                }
            }

            // GET: /Courses/Edit/{id}
            public async Task<IActionResult> Edit(int id)
            {
                try
                {
                    var courses = await _apiService.GetCoursesAsync();
                    var course = courses?.FirstOrDefault(c => c.CourseId == id);

                    if (course == null)
                    {
                        TempData["Error"] = "Course not found.";
                        return RedirectToAction(nameof(Index));
                    }

                    var model = new UpdateCourseDTO
                    {
                        CourseId = course.CourseId,
                        Title = course.Title,
                        Description = course.Description
                    };

                    return View(model);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading course for edit");
                    TempData["Error"] = "Error loading course. Please try again.";
                    return RedirectToAction(nameof(Index));
                }
            }

            // POST: /Courses/Edit/{id}
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Edit(int id, UpdateCourseDTO model)
            {
                if (id != model.CourseId)
                {
                    TempData["Error"] = "Course ID mismatch.";
                    return RedirectToAction(nameof(Index));
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                try
                {
                    var success = await _apiService.UpdateCourseAsync(model);
                    if (success)
                    {
                        TempData["Success"] = "Course updated successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["Error"] = "Failed to update course. Please try again.";
                        return View(model);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating course");
                    TempData["Error"] = "Error updating course. Please try again.";
                    return View(model);
                }
            }

            // POST: /Courses/Delete/{id}
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Delete(int id)
            {
                try
                {
                    var (success, message) = await _apiService.DeleteCourseAsync(id);

                    if (success)
                    {
                        TempData["Success"] = "Course deleted successfully!";
                    }
                    else
                    {
                        TempData["Error"] = message ?? "Failed to delete course. Please try again.";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting course");
                    TempData["Error"] = "Error deleting course. Please try again.";
                }

                return RedirectToAction(nameof(Index));
            }

            // GET: /Courses/Details/{id}
            public async Task<IActionResult> Details(int id)
            {
                try
                {
                    var courses = await _apiService.GetCoursesAsync();
                    var course = courses?.FirstOrDefault(c => c.CourseId == id);

                    if (course == null)
                    {
                        TempData["Error"] = "Course not found.";
                        return RedirectToAction(nameof(Index));
                    }

                    // Get additional course details (modules, tutors, students)
                    var modules = await _apiService.GetModulesByCourseAsync(id);
                    var tutors = await _apiService.GetTutorsByCourseAsync(id);
                    var students = await _apiService.GetStudentsByCourseAsync(id);

                    var viewModel = new CourseDetailsViewModel
                    {
                        Course = course,
                        Modules = modules ?? new List<ModuleDTO>(),
                        Tutors = tutors ?? new List<TutorDTO>(),
                        Students = students ?? new List<StudentDTO>()
                    };

                    return View(viewModel);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading course details");
                    TempData["Error"] = "Error loading course details. Please try again.";
                    return RedirectToAction(nameof(Index));
                }
            }
        }
    }
}
