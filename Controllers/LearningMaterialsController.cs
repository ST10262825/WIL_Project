// Controllers/LearningMaterialsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TutorConnectAPI.Data;
using TutorConnectAPI.DTOs;
using TutorConnectAPI.Models;

namespace TutorConnectAPI.Controllers
{
    [ApiController]
    [Route("api/learning-materials")]
    public class LearningMaterialsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<LearningMaterialsController> _logger;

        public LearningMaterialsController(ApplicationDbContext context, IWebHostEnvironment env, ILogger<LearningMaterialsController> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        // ✅ Get tutor's materials overview (for dashboard)
        [HttpGet("tutor/{tutorId}/overview")]
        public async Task<IActionResult> GetTutorMaterialsOverview(int tutorId)
        {
            try
            {
                _logger.LogInformation("[API DEBUG] Getting materials overview for tutor {TutorId}", tutorId);

                // First, check if tutor exists
                var tutor = await _context.Tutors.FindAsync(tutorId);
                if (tutor == null)
                {
                    _logger.LogWarning("[API DEBUG] Tutor {TutorId} not found", tutorId);
                    return NotFound("Tutor not found");
                }
                _logger.LogInformation("[API DEBUG] Tutor found: {TutorName}", tutor.Name);

                // Check total materials count
                var totalMaterials = await _context.LearningMaterials
                    .CountAsync(m => m.TutorId == tutorId);
                _logger.LogInformation("[API DEBUG] Total materials count: {Count}", totalMaterials);

                // Check total folders count
                var totalFolders = await _context.LearningMaterialFolders
                    .CountAsync(f => f.TutorId == tutorId);
                _logger.LogInformation("[API DEBUG] Total folders count: {Count}", totalFolders);

                // Check root folders
                var rootFolders = await _context.LearningMaterialFolders
                    .Where(f => f.TutorId == tutorId && f.ParentFolderId == null)
                    .Include(f => f.Materials)
                    .Include(f => f.Subfolders)
                    .ToListAsync();

                _logger.LogInformation("[API DEBUG] Root folders found: {Count}", rootFolders.Count);

                foreach (var folder in rootFolders)
                {
                    _logger.LogInformation("[API DEBUG] Folder: {FolderName} (ID: {FolderId}), Materials: {MaterialCount}, Subfolders: {SubfolderCount}",
                        folder.Name, folder.FolderId, folder.Materials.Count, folder.Subfolders.Count);
                }

                var rootFoldersDTO = rootFolders.Select(f => MapFolderToDTO(f)).ToList();

                // Check recent materials
                var recentMaterials = await _context.LearningMaterials
                    .Where(m => m.TutorId == tutorId)
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                _logger.LogInformation("[API DEBUG] Recent materials found: {Count}", recentMaterials.Count);
                var recentMaterialsDTO = recentMaterials.Select(m => MapMaterialToDTO(m)).ToList();

                var totalStorage = await _context.LearningMaterials
                    .Where(m => m.TutorId == tutorId)
                    .SumAsync(m => m.FileSize);

                var overview = new TutorMaterialsOverviewDTO
                {
                    TotalMaterials = totalMaterials,
                    TotalFolders = totalFolders,
                    TotalStorageUsed = totalStorage,
                    TotalStorageFormatted = FormatFileSize(totalStorage),
                    RootFolders = rootFoldersDTO,
                    RecentMaterials = recentMaterialsDTO
                };

                _logger.LogInformation("[API DEBUG] Returning overview - Materials: {Materials}, Folders: {Folders}",
                    overview.TotalMaterials, overview.TotalFolders);

                return Ok(overview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[API DEBUG] Error getting materials overview for tutor {TutorId}", tutorId);
                return StatusCode(500, "Error retrieving materials overview");
            }
        }

        // Add this temporary debug endpoint to your API controller
        [HttpGet("debug/tutor/{tutorId}/folders")]
        public async Task<IActionResult> DebugGetTutorFolders(int tutorId)
        {
            try
            {
                _logger.LogInformation("[DEBUG] Checking all folders for tutor {TutorId}", tutorId);

                // Get ALL folders for this tutor, regardless of parent
                var allFolders = await _context.LearningMaterialFolders
                    .Where(f => f.TutorId == tutorId)
                    .ToListAsync();

                _logger.LogInformation("[DEBUG] Found {Count} total folders for tutor {TutorId}", allFolders.Count, tutorId);

                foreach (var folder in allFolders)
                {
                    _logger.LogInformation("[DEBUG] Folder: ID={FolderId}, Name='{Name}', ParentFolderId={ParentFolderId}, TutorId={TutorId}",
                        folder.FolderId, folder.Name, folder.ParentFolderId, folder.TutorId);
                }

                return Ok(new
                {
                    TotalFolders = allFolders.Count,
                    Folders = allFolders.Select(f => new {
                        f.FolderId,
                        f.Name,
                        f.ParentFolderId,
                        f.TutorId
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DEBUG] Error checking folders for tutor {TutorId}", tutorId);
                return StatusCode(500, "Error checking folders");
            }
        }

        // ✅ Create new folder
        [HttpPost("tutor/{tutorId}/folders")]
        public async Task<IActionResult> CreateFolder(int tutorId, [FromBody] CreateFolderRequest request)
        {
            try
            {
                var tutor = await _context.Tutors.FindAsync(tutorId);
                if (tutor == null) return NotFound("Tutor not found");

                // Check if folder name already exists in this location
                var existingFolder = await _context.LearningMaterialFolders
                    .FirstOrDefaultAsync(f => f.TutorId == tutorId &&
                                            f.ParentFolderId == request.ParentFolderId &&
                                            f.Name == request.Name);

                if (existingFolder != null)
                    return BadRequest("A folder with this name already exists in this location");

                var folder = new LearningMaterialFolder
                {
                    TutorId = tutorId,
                    ParentFolderId = request.ParentFolderId,
                    Name = request.Name,
                    Description = request.Description,
                    CreatedAt = DateTime.UtcNow
                };

                _context.LearningMaterialFolders.Add(folder);
                await _context.SaveChangesAsync();

                return Ok(MapFolderToDTO(folder));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder for tutor {TutorId}", tutorId);
                return StatusCode(500, "Error creating folder");
            }
        }

        // ✅ Upload learning material
        [HttpPost("tutor/{tutorId}/upload")]
        public async Task<IActionResult> UploadMaterial(int tutorId, [FromForm] UploadMaterialRequest request)
        {
            try
            {
                _logger.LogInformation("[API UPLOAD] UploadMaterial called for tutor {TutorId}", tutorId);
                _logger.LogInformation("[API UPLOAD] Request - Title: {Title}, FolderId: {FolderId}, IsPublic: {IsPublic}",
                    request.Title, request.FolderId, request.IsPublic);

                var tutor = await _context.Tutors.FindAsync(tutorId);
                if (tutor == null)
                {
                    _logger.LogWarning("[API UPLOAD] Tutor {TutorId} not found", tutorId);
                    return NotFound("Tutor not found");
                }

                if (request.File == null || request.File.Length == 0)
                {
                    _logger.LogWarning("[API UPLOAD] No file uploaded");
                    return BadRequest("No file uploaded");
                }

                _logger.LogInformation("[API UPLOAD] File details - Name: {FileName}, Size: {FileSize}, ContentType: {ContentType}",
                    request.File.FileName, request.File.Length, request.File.ContentType);

                // Validate file size (10MB max)
                if (request.File.Length > 10 * 1024 * 1024)
                {
                    _logger.LogWarning("[API UPLOAD] File too large: {FileSize} bytes", request.File.Length);
                    return BadRequest("File size cannot exceed 10MB");
                }

                // Validate file type
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".txt", ".jpg", ".png", ".mp4", ".mp3" };
                var fileExtension = Path.GetExtension(request.File.FileName).ToLower();
                _logger.LogInformation("[API UPLOAD] File extension: {FileExtension}", fileExtension);

                if (!allowedExtensions.Contains(fileExtension))
                {
                    _logger.LogWarning("[API UPLOAD] Invalid file type: {FileExtension}", fileExtension);
                    return BadRequest("File type not allowed");
                }

                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", "learning-materials", tutorId.ToString());
                _logger.LogInformation("[API UPLOAD] Upload path: {UploadPath}", uploadsPath);

                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                    _logger.LogInformation("[API UPLOAD] Created directory: {UploadPath}", uploadsPath);
                }

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, fileName);
                _logger.LogInformation("[API UPLOAD] Saving file to: {FilePath}", filePath);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.File.CopyToAsync(stream);
                }

                _logger.LogInformation("[API UPLOAD] File saved successfully");

                // Create material record
                var material = new LearningMaterial
                {
                    TutorId = tutorId,
                    FolderId = request.FolderId,
                    Title = request.Title,
                    Description = request.Description,
                    FileName = request.File.FileName,
                    FilePath = filePath,
                    FileType = fileExtension.Replace(".", "").ToUpper(),
                    FileSize = request.File.Length,
                    IsPublic = request.IsPublic,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("[API UPLOAD] Creating material record: {MaterialTitle}", material.Title);

                _context.LearningMaterials.Add(material);
                var changes = await _context.SaveChangesAsync();

                _logger.LogInformation("[API UPLOAD] SaveChanges completed. Changes: {Changes}, MaterialId: {MaterialId}",
                    changes, material.LearningMaterialId);

                var materialDTO = MapMaterialToDTO(material);
                _logger.LogInformation("[API UPLOAD] Upload completed successfully. Material ID: {MaterialId}", material.LearningMaterialId);

                return Ok(materialDTO);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[API UPLOAD] Error uploading material for tutor {TutorId}", tutorId);
                return StatusCode(500, "Error uploading material");
            }
        }



        // ✅ Get folder contents
        [HttpGet("tutor/{tutorId}/folders/{folderId}")]
        public async Task<IActionResult> GetFolderContents(int tutorId, int folderId)
        {
            try
            {
                var folder = await _context.LearningMaterialFolders
                    .Include(f => f.Materials)
                    .Include(f => f.Subfolders)
                    .FirstOrDefaultAsync(f => f.FolderId == folderId && f.TutorId == tutorId);

                if (folder == null) return NotFound("Folder not found");

                return Ok(MapFolderToDTO(folder));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folder contents for folder {FolderId}", folderId);
                return StatusCode(500, "Error retrieving folder contents");
            }
        }

        // ✅ Delete material
        [HttpDelete("materials/{materialId}")]
        public async Task<IActionResult> DeleteMaterial(int materialId)
        {
            try
            {
                var material = await _context.LearningMaterials.FindAsync(materialId);
                if (material == null) return NotFound("Material not found");

                // Delete physical file
                if (System.IO.File.Exists(material.FilePath))
                {
                    System.IO.File.Delete(material.FilePath);
                }

                _context.LearningMaterials.Remove(material);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Material deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting material {MaterialId}", materialId);
                return StatusCode(500, "Error deleting material");
            }
        }


        // ✅ Download material (for students AND tutors)
        // ✅ Download material (for students)
        [HttpGet("materials/{materialId}/download")]
        public async Task<IActionResult> DownloadMaterial(int materialId, [FromQuery] int studentId)
        {
            try
            {
                var material = await _context.LearningMaterials
                    .Include(m => m.Tutor)
                    .FirstOrDefaultAsync(m => m.LearningMaterialId == materialId);

                if (material == null)
                {
                    return NotFound("Material not found");
                }

                // Check access: public OR student has accessible booking with tutor
                var hasAccess = material.IsPublic ||
                               await _context.Bookings.AnyAsync(b =>
                                   b.StudentId == studentId &&
                                   b.TutorId == material.TutorId &&
                                   (b.Status == "Accepted" ||
                                    (b.Status == "Completed" &&
                                     b.CompletedAt.HasValue &&
                                     b.CompletedAt.Value.AddDays(2) >= DateTime.UtcNow)));

                if (!hasAccess)
                {
                    return StatusCode(403, "You don't have access to this material. Book a session or your access period may have expired.");
                }

                if (!System.IO.File.Exists(material.FilePath))
                {
                    return NotFound("File not found on server");
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(material.FilePath);
                return File(fileBytes, "application/octet-stream", material.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading material {MaterialId}", materialId);
                return StatusCode(500, "Error downloading material");
            }
        }

        // ✅ Delete folder (only if empty)
        [HttpDelete("folders/{folderId}")]
        public async Task<IActionResult> DeleteFolder(int folderId)
        {
            try
            {
                _logger.LogInformation("[API] Deleting folder {FolderId}", folderId);

                var folder = await _context.LearningMaterialFolders
                    .Include(f => f.Materials)
                    .Include(f => f.Subfolders)
                    .FirstOrDefaultAsync(f => f.FolderId == folderId);

                if (folder == null)
                {
                    _logger.LogWarning("[API] Folder {FolderId} not found", folderId);
                    return NotFound("Folder not found");
                }

                // Check if folder has any materials
                if (folder.Materials.Any())
                {
                    _logger.LogWarning("[API] Folder {FolderId} has {Count} materials - cannot delete", folderId, folder.Materials.Count);
                    return BadRequest("Cannot delete folder that contains materials. Please delete or move all materials first.");
                }

                // Check if folder has any subfolders
                if (folder.Subfolders.Any())
                {
                    _logger.LogWarning("[API] Folder {FolderId} has {Count} subfolders - cannot delete", folderId, folder.Subfolders.Count);
                    return BadRequest("Cannot delete folder that contains subfolders. Please delete all subfolders first.");
                }

                _context.LearningMaterialFolders.Remove(folder);
                await _context.SaveChangesAsync();

                _logger.LogInformation("[API] Folder {FolderId} deleted successfully", folderId);
                return Ok(new { message = "Folder deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[API] Error deleting folder {FolderId}", folderId);
                return StatusCode(500, "Error deleting folder");
            }
        }

        // Helper methods
        private LearningMaterialDTO MapMaterialToDTO(LearningMaterial material)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var fileUrl = $"{baseUrl}/api/learning-materials/materials/{material.LearningMaterialId}/download";

            return new LearningMaterialDTO
            {
                LearningMaterialId = material.LearningMaterialId,
                TutorId = material.TutorId,
                FolderId = material.FolderId,
                Title = material.Title,
                Description = material.Description,
                FileName = material.FileName,
                FileUrl = fileUrl,
                FileType = material.FileType,
                FileSize = material.FileSize,
                FileSizeFormatted = FormatFileSize(material.FileSize),
                IsPublic = material.IsPublic,
                CreatedAt = material.CreatedAt,
                UpdatedAt = material.UpdatedAt
            };
        }

        private LearningMaterialFolderDTO MapFolderToDTO(LearningMaterialFolder folder)
        {
            return new LearningMaterialFolderDTO
            {
                FolderId = folder.FolderId,
                TutorId = folder.TutorId,
                ParentFolderId = folder.ParentFolderId,
                Name = folder.Name,
                Description = folder.Description,
                CreatedAt = folder.CreatedAt,
                MaterialCount = folder.Materials.Count,
                SubfolderCount = folder.Subfolders.Count,
                Materials = folder.Materials.Select(MapMaterialToDTO).ToList(),
                Subfolders = folder.Subfolders.Select(MapFolderToDTO).ToList()
            };
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double len = bytes;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private async Task<bool> HasAccessThroughBooking(int studentId, int tutorId)
        {
            return await _context.Bookings
                .AnyAsync(b => b.StudentId == studentId &&
                             b.TutorId == tutorId &&
                             b.Status == "Completed");
        }
    }
}