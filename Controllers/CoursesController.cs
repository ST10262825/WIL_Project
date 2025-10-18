using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorConnectAPI.Data;
using TutorConnectAPI.DTOs;
using TutorConnectAPI.Models;

namespace TutorConnectAPI.Controllers
{
    [ApiController]
    [Route("api/admin/courses")]
    //[Authorize(Roles = "Admin")]
    public class CoursesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CoursesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCourses()
        {
            var courses = await _context.Courses
                .Select(c => new CourseDTO
                {
                    CourseId = c.CourseId,
                    Title = c.Title,
                    Description = c.Description,
                    ModuleCount = c.Modules.Count,
                    TutorCount = c.Tutors.Count,
                    StudentCount = c.Students.Count
                })
                .ToListAsync();

            return Ok(courses);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDTO dto)
        {
            var course = new Course
            {
                Title = dto.Title,
                Description = dto.Description
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Course created successfully", courseId = course.CourseId });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourseDTO dto)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            course.Title = dto.Title;
            course.Description = dto.Description;

            await _context.SaveChangesAsync();
            return Ok("Course updated successfully");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Modules)
                .Include(c => c.Tutors)
                .Include(c => c.Students)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null) return NotFound();

            if (course.Modules.Any() || course.Tutors.Any() || course.Students.Any())
                return BadRequest("Cannot delete course with associated modules, tutors, or students");

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            return Ok("Course deleted successfully");
        }
    }
}
