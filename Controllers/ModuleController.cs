using Microsoft.AspNetCore.Mvc;

namespace TutorConnectAPI.Controllers
{
    using global::TutorConnectAPI.Data;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    

    namespace TutorConnectAPI.Controllers
    {
        [ApiController]
        [Route("api/module")]
        public class ModulesController : ControllerBase
        {
            private readonly ApplicationDbContext _context;

            public ModulesController(ApplicationDbContext context)
            {
                _context = context;
            }

            [HttpGet]
            public async Task<IActionResult> GetModules()
            {
                var modules = await _context.Modules.ToListAsync();
                return Ok(modules);
            }
        }
    }

}
